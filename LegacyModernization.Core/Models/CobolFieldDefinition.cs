using System;
using System.Collections.Generic;

namespace LegacyModernization.Core.Models
{
    /// <summary>
    /// Represents a COBOL field definition with its data type, size, and position
    /// </summary>
    public class CobolFieldDefinition
    {
        public string Name { get; set; } = string.Empty;
        public int Level { get; set; }
        public string Picture { get; set; } = string.Empty;
        public CobolDataType DataType { get; set; }
        public int Length { get; set; }
        public int DecimalPlaces { get; set; }
        public bool IsComputed { get; set; }
        public int Position { get; set; }
        public string DefaultValue { get; set; } = string.Empty;
        public List<CobolFieldDefinition> Children { get; set; } = new List<CobolFieldDefinition>();
        public CobolFieldDefinition? Parent { get; set; }
    }

    /// <summary>
    /// COBOL data types for proper field handling
    /// </summary>
    public enum CobolDataType
    {
        Alphanumeric,    // PIC X
        Numeric,         // PIC 9
        SignedNumeric,   // PIC S9
        Packed,          // COMP-3
        Binary,          // COMP
        Display,         // Default display format
        Group            // Group item (01, 05 level items)
    }

    /// <summary>
    /// Represents the complete MB2000 record structure based on COBOL definitions
    /// </summary>
    public class MB2000RecordStructure
    {
        public List<CobolFieldDefinition> Fields { get; set; } = new List<CobolFieldDefinition>();
        public int TotalLength { get; set; }
        
        /// <summary>
        /// Get a field by its COBOL name
        /// </summary>
        public CobolFieldDefinition? GetField(string name)
        {
            return FindFieldRecursive(Fields, name);
        }
        
        private CobolFieldDefinition? FindFieldRecursive(List<CobolFieldDefinition> fields, string name)
        {
            foreach (var field in fields)
            {
                if (field.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    return field;
                    
                var found = FindFieldRecursive(field.Children, name);
                if (found != null)
                    return found;
            }
            return null;
        }
    }
}
