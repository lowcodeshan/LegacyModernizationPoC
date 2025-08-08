using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using LegacyModernization.Core.Models;
using Serilog;

namespace LegacyModernization.Core.Utilities
{
    /// <summary>
    /// Parses COBOL data structure definitions to create field mapping
    /// </summary>
    public class CobolStructureParser
    {
        private readonly ILogger _logger;
        private const string CobolDataStructurePath = @"c:\Users\Shan\Documents\Legacy Mordernization\CONTAINER_LIBRARY\CONTAINER_LIBRARY\mblps\mblps.dd.cbl";

        public CobolStructureParser(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Parse the MB2000 COBOL structure from actual COBOL copybook file with hierarchical position calculation
        /// </summary>
        public MB2000RecordStructure ParseMB2000Structure()
        {
            _logger.Information("Parsing COBOL structure from file: {FilePath}", CobolDataStructurePath);
            
            if (!File.Exists(CobolDataStructurePath))
            {
                _logger.Error("COBOL structure file not found: {FilePath}", CobolDataStructurePath);
                throw new FileNotFoundException($"COBOL structure file not found: {CobolDataStructurePath}");
            }

            var structure = new MB2000RecordStructure();
            var lines = File.ReadAllLines(CobolDataStructurePath);
            
            // Parse hierarchically to calculate correct positions
            var parsedFields = ParseCobolHierarchy(lines);
            
            // Calculate positions for elementary items only
            CalculateFieldPositions(parsedFields);
            
            // Add only elementary items to structure (items with actual data)
            var elementaryFields = GetElementaryFields(parsedFields);
            foreach (var field in elementaryFields)
            {
                structure.Fields.Add(field);
                _logger.Debug("Added field: {Name} at position {Position}, length {Length}, type {DataType}", 
                    field.Name, field.Position, field.Length, field.DataType);
            }

            structure.TotalLength = elementaryFields.Any() ? 
                elementaryFields.Max(f => f.Position + f.Length - 1) : 0;
            _logger.Information("Parsed COBOL structure: {FieldCount} elementary fields, total length {Length}", 
                structure.Fields.Count, structure.TotalLength);

            return structure;
        }
        
        /// <summary>
        /// Get all elementary fields from hierarchical structure
        /// </summary>
        private List<CobolFieldDefinition> GetElementaryFields(List<CobolFieldDefinition> fields)
        {
            var elementaryFields = new List<CobolFieldDefinition>();
            GetElementaryFieldsRecursive(fields, elementaryFields);
            return elementaryFields;
        }
        
        private void GetElementaryFieldsRecursive(List<CobolFieldDefinition> fields, List<CobolFieldDefinition> elementaryFields)
        {
            foreach (var field in fields)
            {
                if (field.DataType != CobolDataType.Group)
                {
                    elementaryFields.Add(field);
                }
                else if (field.Children.Count > 0)
                {
                    GetElementaryFieldsRecursive(field.Children, elementaryFields);
                }
            }
        }

        /// <summary>
        /// Parse COBOL structure hierarchically to maintain parent-child relationships
        /// </summary>
        private List<CobolFieldDefinition> ParseCobolHierarchy(string[] lines)
        {
            var fields = new List<CobolFieldDefinition>();
            var stack = new Stack<CobolFieldDefinition>();
            
            foreach (var line in lines)
            {
                if (IsCobolFieldDefinition(line))
                {
                    var field = ParseCobolLine(line, 0); // Position will be calculated later
                    if (field != null)
                    {
                        // Build hierarchy based on levels
                        while (stack.Count > 0 && stack.Peek().Level >= field.Level)
                        {
                            stack.Pop();
                        }
                        
                        if (stack.Count > 0)
                        {
                            var parent = stack.Peek();
                            field.Parent = parent;
                            parent.Children.Add(field);
                        }
                        else
                        {
                            fields.Add(field);
                        }
                        
                        stack.Push(field);
                    }
                }
            }
            
            return fields;
        }

        /// <summary>
        /// Calculate actual byte positions for COBOL fields based on hierarchy
        /// </summary>
        private void CalculateFieldPositions(List<CobolFieldDefinition> fields)
        {
            CalculatePositionsRecursive(fields, 1); // COBOL positions start at 1
        }
        
        private int CalculatePositionsRecursive(List<CobolFieldDefinition> fields, int startPosition)
        {
            var currentPosition = startPosition;
            
            foreach (var field in fields)
            {
                field.Position = currentPosition;
                
                if (field.DataType == CobolDataType.Group && field.Children.Count > 0)
                {
                    // Group items: calculate position from children
                    var childEndPosition = CalculatePositionsRecursive(field.Children, currentPosition);
                    field.Length = childEndPosition - currentPosition;
                    currentPosition = childEndPosition;
                }
                else if (field.DataType != CobolDataType.Group)
                {
                    // Elementary items: consume actual bytes
                    currentPosition += field.Length;
                }
            }
            
            return currentPosition;
        }

        private bool IsCobolFieldDefinition(string line)
        {
            // Skip comment lines and empty lines
            if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("*"))
                return false;

            var trimmed = line.Trim();
            
            // Match COBOL field definitions with level numbers (01, 05, 10, 15, 20)
            // Handle various patterns like:
            // "01 MB-REC.", "05 MB-CLIENT-ID-FIELDS.", "10 MB-CLIENT.", "15 MB-CLIENT3 PIC 9(3)."
            return Regex.IsMatch(trimmed, @"^\d{2}\s+[\w-]+(\s+PIC\s+|\.|\s+REDEFINES\s+|$)") &&
                   !trimmed.Contains("REDEFINES"); // Skip REDEFINES for now to avoid duplicates
        }

        private CobolFieldDefinition? ParseCobolLine(string line, int position)
        {
            try
            {
                var trimmed = line.Trim();
                
                // Parse COBOL field definitions with comprehensive regex
                // Handle patterns like:
                // "15 MB-CLIENT3 PIC 9(3).", "10 MB-ACCOUNT PIC S9(13) COMP-3.", "05 MB-CLIENT-ID-FIELDS."
                var match = Regex.Match(trimmed, @"^(\d{2})\s+([\w-]+)(?:\s+PIC\s+([\w\(\)VS]+))?(?:\s+(COMP-?\d*))?\.?");
                
                if (!match.Success) return null;

                var field = new CobolFieldDefinition
                {
                    Level = int.Parse(match.Groups[1].Value),
                    Name = match.Groups[2].Value,
                    Position = 0 // Will be calculated later in hierarchical processing
                };

                // Parse PIC clause if present
                if (match.Groups[3].Success)
                {
                    field.Picture = match.Groups[3].Value;
                    ParsePictureClause(field);
                }
                else
                {
                    // Group item - calculate length based on subordinate items
                    field.DataType = CobolDataType.Group;
                    field.Length = 0; // Will be calculated later
                }

                // Handle COMP fields
                if (match.Groups[4].Success)
                {
                    var compType = match.Groups[4].Value;
                    if (compType.Contains("3") || compType == "COMP-3")
                    {
                        field.DataType = CobolDataType.Packed;
                        field.IsComputed = true;
                        // For COMP-3, calculate actual storage length
                        if (field.Picture != null)
                        {
                            var digits = ExtractDigitCount(field.Picture);
                            field.Length = (digits + 1) / 2 + 1; // COMP-3 formula
                        }
                    }
                    else
                    {
                        field.DataType = CobolDataType.Binary;
                        field.IsComputed = true;
                    }
                }

                return field;
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Failed to parse COBOL line: {Line}", line);
                return null;
            }
        }

        private void ParsePictureClause(CobolFieldDefinition field)
        {
            var pic = field.Picture;
            
            // Handle signed fields
            if (pic.StartsWith("S"))
            {
                field.DataType = CobolDataType.SignedNumeric;
                pic = pic.Substring(1);
            }
            
            // Parse field types and lengths
            if (pic.StartsWith("X"))
            {
                field.DataType = field.DataType == CobolDataType.SignedNumeric ? 
                    CobolDataType.SignedNumeric : CobolDataType.Alphanumeric;
                field.Length = ExtractLength(pic);
            }
            else if (pic.StartsWith("9"))
            {
                field.DataType = field.DataType == CobolDataType.SignedNumeric ? 
                    CobolDataType.SignedNumeric : CobolDataType.Numeric;
                field.Length = ExtractLength(pic);
                
                // Handle decimal places (V9(2) or V99)
                var decimalMatch = Regex.Match(pic, @"V9+\(?(\d+)?\)?");
                if (decimalMatch.Success)
                {
                    if (decimalMatch.Groups[1].Success)
                    {
                        field.DecimalPlaces = int.Parse(decimalMatch.Groups[1].Value);
                    }
                    else
                    {
                        // Count V9s manually
                        var vIndex = pic.IndexOf('V');
                        if (vIndex >= 0)
                        {
                            var afterV = pic.Substring(vIndex + 1);
                            field.DecimalPlaces = afterV.TakeWhile(c => c == '9').Count();
                        }
                    }
                }
            }
        }

        private int ExtractDigitCount(string pic)
        {
            // Extract total digit count from patterns like S9(13)V99, 9(3), etc.
            var totalDigits = 0;
            
            // Remove S prefix if present
            var cleanPic = pic.StartsWith("S") ? pic.Substring(1) : pic;
            
            // Find all digit patterns
            var matches = Regex.Matches(cleanPic, @"9+\(?(\d+)?\)?");
            foreach (Match match in matches)
            {
                if (match.Groups[1].Success)
                {
                    totalDigits += int.Parse(match.Groups[1].Value);
                }
                else
                {
                    totalDigits += match.Value.Count(c => c == '9');
                }
            }
            
            return totalDigits;
        }

        private int ExtractLength(string pic)
        {
            // Extract length from patterns like X(60), 9(7), etc.
            var match = Regex.Match(pic, @"\((\d+)\)");
            if (match.Success)
            {
                return int.Parse(match.Groups[1].Value);
            }
            
            // Count repeated characters like XXX or 999
            var charMatch = Regex.Match(pic, @"^([X9]+)");
            if (charMatch.Success)
            {
                return charMatch.Groups[1].Value.Length;
            }
            
            return 1; // Default
        }
    }
}
