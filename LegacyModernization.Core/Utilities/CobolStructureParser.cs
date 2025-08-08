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
        /// Parse the MB2000 COBOL structure from the actual COBOL file
        /// </summary>
        public MB2000RecordStructure ParseMB2000Structure()
        {
            _logger.Information("Parsing COBOL structure from {Path}", CobolDataStructurePath);
            
            if (!File.Exists(CobolDataStructurePath))
            {
                _logger.Warning("COBOL structure file not found at {Path}, using hardcoded structure", CobolDataStructurePath);
                return CreateHardcodedStructure();
            }

            var lines = File.ReadAllLines(CobolDataStructurePath);
            var structure = new MB2000RecordStructure();
            var fieldStack = new Stack<CobolFieldDefinition>();
            int currentPosition = 1;

            foreach (var line in lines)
            {
                if (IsCobolFieldDefinition(line))
                {
                    var field = ParseCobolLine(line, currentPosition);
                    if (field != null)
                    {
                        // Handle hierarchy
                        while (fieldStack.Count > 0 && fieldStack.Peek().Level >= field.Level)
                        {
                            fieldStack.Pop();
                        }

                        if (fieldStack.Count > 0)
                        {
                            field.Parent = fieldStack.Peek();
                            fieldStack.Peek().Children.Add(field);
                        }
                        else
                        {
                            structure.Fields.Add(field);
                        }

                        fieldStack.Push(field);
                        
                        // Update position for next field
                        if (field.DataType != CobolDataType.Group)
                        {
                            currentPosition += field.Length;
                        }
                    }
                }
            }

            structure.TotalLength = currentPosition - 1;
            _logger.Information("Parsed COBOL structure with {FieldCount} fields, total length {Length}", 
                structure.Fields.Count, structure.TotalLength);
            
            return structure;
        }

        private bool IsCobolFieldDefinition(string line)
        {
            // Match COBOL field definitions like "05 MB-CLIENT PIC X(4)."
            return Regex.IsMatch(line.Trim(), @"^\d{2}\s+[\w-]+(\s+PIC\s+|\.|\s+REDEFINES\s+)");
        }

        private CobolFieldDefinition? ParseCobolLine(string line, int position)
        {
            try
            {
                var trimmed = line.Trim();
                
                // Parse: "05 MB-CLIENT-ID-FIELDS."
                var match = Regex.Match(trimmed, @"^(\d{2})\s+([\w-]+)(?:\s+PIC\s+([\w\(\)V]+))?(?:\s+COMP-?(\d+)?)?\.?");
                
                if (!match.Success) return null;

                var field = new CobolFieldDefinition
                {
                    Level = int.Parse(match.Groups[1].Value),
                    Name = match.Groups[2].Value,
                    Position = position
                };

                // Parse PIC clause if present
                if (match.Groups[3].Success)
                {
                    field.Picture = match.Groups[3].Value;
                    ParsePictureClause(field);
                }
                else
                {
                    // Group item
                    field.DataType = CobolDataType.Group;
                    field.Length = 0;
                }

                // Handle COMP fields
                if (match.Groups[4].Success)
                {
                    var compType = match.Groups[4].Value;
                    if (compType == "3")
                    {
                        field.DataType = CobolDataType.Packed;
                        field.IsComputed = true;
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
                
                // Handle decimal places (V9(2))
                var decimalMatch = Regex.Match(pic, @"V9\((\d+)\)");
                if (decimalMatch.Success)
                {
                    field.DecimalPlaces = int.Parse(decimalMatch.Groups[1].Value);
                }
            }
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

        /// <summary>
        /// Create a hardcoded structure based on the key fields we know from the COBOL program
        /// This is used as a fallback when the COBOL file isn't available
        /// </summary>
        private MB2000RecordStructure CreateHardcodedStructure()
        {
            var structure = new MB2000RecordStructure();
            
            // Based on the COBOL mblps.dd.cbl structure, create key field definitions
            structure.Fields.AddRange(new[]
            {
                new CobolFieldDefinition { Name = "MB-CLIENT3", Level = 15, DataType = CobolDataType.Numeric, Length = 3, Position = 1 },
                new CobolFieldDefinition { Name = "MB-ACCOUNT", Level = 10, DataType = CobolDataType.Packed, Length = 8, Position = 11 },
                new CobolFieldDefinition { Name = "MB-FORMATTED-ACCOUNT", Level = 15, DataType = CobolDataType.Numeric, Length = 10, Position = 19 },
                new CobolFieldDefinition { Name = "MB-SSN", Level = 10, DataType = CobolDataType.Packed, Length = 5, Position = 29 },
                new CobolFieldDefinition { Name = "MB-BILL-NAME", Level = 10, DataType = CobolDataType.Alphanumeric, Length = 60, Position = 35 },
                new CobolFieldDefinition { Name = "MB-BILL-LINE-2", Level = 10, DataType = CobolDataType.Alphanumeric, Length = 60, Position = 95 },
                new CobolFieldDefinition { Name = "MB-BILL-LINE-3", Level = 10, DataType = CobolDataType.Alphanumeric, Length = 60, Position = 155 },
                new CobolFieldDefinition { Name = "MB-BILL-CITY", Level = 15, DataType = CobolDataType.Alphanumeric, Length = 51, Position = 275 },
                new CobolFieldDefinition { Name = "MB-BILL-STATE", Level = 15, DataType = CobolDataType.Alphanumeric, Length = 2, Position = 326 },
                new CobolFieldDefinition { Name = "MB-ZIP-5", Level = 15, DataType = CobolDataType.Alphanumeric, Length = 5, Position = 328 },
                new CobolFieldDefinition { Name = "MB-ZIP-4", Level = 15, DataType = CobolDataType.Alphanumeric, Length = 4, Position = 333 },
                new CobolFieldDefinition { Name = "MB-TELE-NO", Level = 15, DataType = CobolDataType.Alphanumeric, Length = 12, Position = 367 }
            });

            structure.TotalLength = 2000; // Standard MB2000 record length
            return structure;
        }
    }
}
