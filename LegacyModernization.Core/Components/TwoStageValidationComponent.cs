using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Serilog;

namespace LegacyModernization.Core.Components
{
    /// <summary>
    /// Enhanced validation component for two-stage conversion architecture
    /// Validates Binary→ASCII→MB2000 conversion pipeline accuracy
    /// </summary>
    public class TwoStageValidationComponent
    {
        private readonly ILogger _logger;

        public TwoStageValidationComponent(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Validate two-stage conversion results against expected output
        /// </summary>
        /// <param name="binaryInputPath">Original binary .dat file</param>
        /// <param name="asciiIntermediatePath">ASCII intermediate .asc file</param>
        /// <param name="mb2000OutputPath">Final MB2000 output file</param>
        /// <param name="expectedOutputPath">Expected output for comparison</param>
        /// <returns>Comprehensive validation results</returns>
        public async Task<TwoStageValidationResult> ValidateConversionPipelineAsync(
            string binaryInputPath, 
            string asciiIntermediatePath, 
            string mb2000OutputPath, 
            string expectedOutputPath)
        {
            var result = new TwoStageValidationResult
            {
                ValidationStartTime = DateTime.Now
            };

            try
            {
                _logger.Information("=== Two-Stage Conversion Validation ===");
                _logger.Information("Binary: {BinaryPath}", binaryInputPath);
                _logger.Information("ASCII: {AsciiPath}", asciiIntermediatePath);
                _logger.Information("MB2000: {MB2000Path}", mb2000OutputPath);
                _logger.Information("Expected: {ExpectedPath}", expectedOutputPath);

                // Stage 1 Validation: Binary → ASCII conversion accuracy
                result.Stage1Validation = await ValidateBinaryToAsciiStageAsync(binaryInputPath, asciiIntermediatePath);
                
                // Stage 2 Validation: ASCII → MB2000 conversion accuracy  
                result.Stage2Validation = await ValidateAsciiToMB2000StageAsync(asciiIntermediatePath, mb2000OutputPath);
                
                // Overall Pipeline Validation: End-to-end accuracy
                result.OverallValidation = await ValidateOverallPipelineAsync(mb2000OutputPath, expectedOutputPath);
                
                // Calculate combined metrics
                result.OverallAccuracy = CalculateOverallAccuracy(result);
                result.ValidationEndTime = DateTime.Now;
                result.Success = result.OverallAccuracy >= 95.0; // Target threshold

                _logger.Information("=== Validation Complete ===");
                _logger.Information("Stage 1 Accuracy: {Stage1}%", result.Stage1Validation.AccuracyPercentage);
                _logger.Information("Stage 2 Accuracy: {Stage2}%", result.Stage2Validation.AccuracyPercentage);
                _logger.Information("Overall Accuracy: {Overall}%", result.OverallAccuracy);

                return result;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Two-stage validation failed");
                result.Success = false;
                result.Error = ex.Message;
                result.ValidationEndTime = DateTime.Now;
                return result;
            }
        }

        /// <summary>
        /// Validate Binary → ASCII conversion (Stage 1)
        /// </summary>
        private async Task<StageValidationResult> ValidateBinaryToAsciiStageAsync(string binaryPath, string asciiPath)
        {
            var result = new StageValidationResult { StageName = "Binary-to-ASCII" };
            
            try
            {
                // Check ASCII file format and structure
                var asciiRecords = await ReadAsciiRecordsForValidation(asciiPath);
                result.RecordCount = asciiRecords.Count;
                
                // Validate ASCII record format (1500 characters each)
                var formatValidation = ValidateAsciiRecordFormat(asciiRecords);
                result.FormatAccuracy = formatValidation.accuracy;
                result.FormatIssues = formatValidation.issues;
                
                // Validate field extraction accuracy by checking known patterns
                var fieldValidation = ValidateAsciiFieldExtraction(asciiRecords);
                result.FieldAccuracy = fieldValidation.accuracy;
                result.FieldIssues = fieldValidation.issues;
                
                result.AccuracyPercentage = (result.FormatAccuracy + result.FieldAccuracy) / 2.0;
                result.Success = result.AccuracyPercentage >= 90.0;
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Stage 1 validation failed");
                result.Success = false;
                result.Error = ex.Message;
                return result;
            }
        }

        /// <summary>
        /// Validate ASCII → MB2000 conversion (Stage 2)
        /// </summary>
        private async Task<StageValidationResult> ValidateAsciiToMB2000StageAsync(string asciiPath, string mb2000Path)
        {
            var result = new StageValidationResult { StageName = "ASCII-to-MB2000" };
            
            try
            {
                var asciiRecords = await ReadAsciiRecordsForValidation(asciiPath);
                var mb2000Records = await File.ReadAllLinesAsync(mb2000Path);
                
                result.RecordCount = mb2000Records.Length;
                
                // Validate record count consistency
                if (asciiRecords.Count != mb2000Records.Length)
                {
                    result.FormatIssues.Add($"Record count mismatch: ASCII={asciiRecords.Count}, MB2000={mb2000Records.Length}");
                }
                
                // Validate MB2000 field structure and accuracy
                var structureValidation = ValidateMB2000Structure(mb2000Records);
                result.FormatAccuracy = structureValidation.accuracy;
                result.FormatIssues.AddRange(structureValidation.issues);
                
                // Cross-validate ASCII→MB2000 field mapping
                var mappingValidation = ValidateAsciiToMB2000Mapping(asciiRecords, mb2000Records);
                result.FieldAccuracy = mappingValidation.accuracy;
                result.FieldIssues.AddRange(mappingValidation.issues);
                
                result.AccuracyPercentage = (result.FormatAccuracy + result.FieldAccuracy) / 2.0;
                result.Success = result.AccuracyPercentage >= 90.0;
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Stage 2 validation failed");
                result.Success = false;
                result.Error = ex.Message;
                return result;
            }
        }

        /// <summary>
        /// Validate overall pipeline accuracy against expected output
        /// </summary>
        private async Task<StageValidationResult> ValidateOverallPipelineAsync(string mb2000Path, string expectedPath)
        {
            var result = new StageValidationResult { StageName = "Overall-Pipeline" };
            
            try
            {
                var actualRecords = await File.ReadAllLinesAsync(mb2000Path);
                var expectedRecords = await File.ReadAllLinesAsync(expectedPath);
                
                result.RecordCount = actualRecords.Length;
                
                // Field-by-field comparison
                var comparisonResult = CompareFieldAccuracy(actualRecords, expectedRecords);
                result.FieldAccuracy = comparisonResult.accuracy;
                result.FieldIssues = comparisonResult.issues;
                
                result.AccuracyPercentage = result.FieldAccuracy;
                result.Success = result.AccuracyPercentage >= 95.0; // Higher threshold for overall
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Overall pipeline validation failed");
                result.Success = false;
                result.Error = ex.Message;
                return result;
            }
        }

        /// <summary>
        /// Calculate overall accuracy across all validation stages
        /// </summary>
        private double CalculateOverallAccuracy(TwoStageValidationResult result)
        {
            var accuracies = new List<double>();
            
            if (result.Stage1Validation.Success)
                accuracies.Add(result.Stage1Validation.AccuracyPercentage);
            
            if (result.Stage2Validation.Success)
                accuracies.Add(result.Stage2Validation.AccuracyPercentage);
            
            if (result.OverallValidation.Success)
                accuracies.Add(result.OverallValidation.AccuracyPercentage);
            
            return accuracies.Any() ? accuracies.Average() : 0.0;
        }

        // Helper methods for specific validations...
        private async Task<List<string>> ReadAsciiRecordsForValidation(string asciiPath)
        {
            var records = new List<string>();
            using var fileStream = new FileStream(asciiPath, FileMode.Open, FileAccess.Read);
            using var reader = new BinaryReader(fileStream);

            const int recordSize = 1500;
            while (fileStream.Position < fileStream.Length)
            {
                var remainingBytes = fileStream.Length - fileStream.Position;
                if (remainingBytes < recordSize) break;

                var recordBytes = reader.ReadBytes(recordSize);
                var asciiRecord = System.Text.Encoding.ASCII.GetString(recordBytes);
                records.Add(asciiRecord);
            }

            return records;
        }

        private (double accuracy, List<string> issues) ValidateAsciiRecordFormat(List<string> records)
        {
            var issues = new List<string>();
            int validRecords = 0;

            foreach (var record in records)
            {
                if (record.Length == 1500)
                {
                    validRecords++;
                }
                else
                {
                    issues.Add($"Invalid record length: {record.Length} (expected 1500)");
                }
            }

            var accuracy = records.Count > 0 ? (validRecords * 100.0) / records.Count : 0.0;
            return (accuracy, issues);
        }

        private (double accuracy, List<string> issues) ValidateAsciiFieldExtraction(List<string> records)
        {
            var issues = new List<string>();
            int validFields = 0;
            int totalFields = 0;

            foreach (var record in records)
            {
                // Check key fields for valid data patterns
                var clientNo = record.Length > 3 ? record.Substring(0, 3) : "";
                var loanNo = record.Length > 11 ? record.Substring(4, 7) : "";
                
                totalFields += 2;
                
                if (!string.IsNullOrWhiteSpace(clientNo) && clientNo.All(char.IsDigit))
                    validFields++;
                else
                    issues.Add($"Invalid client number: '{clientNo}'");
                
                if (!string.IsNullOrWhiteSpace(loanNo))
                    validFields++;
                else
                    issues.Add($"Empty loan number");
            }

            var accuracy = totalFields > 0 ? (validFields * 100.0) / totalFields : 0.0;
            return (accuracy, issues);
        }

        private (double accuracy, List<string> issues) ValidateMB2000Structure(string[] records)
        {
            var issues = new List<string>();
            int validRecords = 0;

            foreach (var record in records)
            {
                var fields = record.Split('|');
                if (fields.Length >= 10) // Minimum expected fields
                {
                    validRecords++;
                }
                else
                {
                    issues.Add($"Insufficient fields: {fields.Length} (expected 10+)");
                }
            }

            var accuracy = records.Length > 0 ? (validRecords * 100.0) / records.Length : 0.0;
            return (accuracy, issues);
        }

        private (double accuracy, List<string> issues) ValidateAsciiToMB2000Mapping(List<string> asciiRecords, string[] mb2000Records)
        {
            var issues = new List<string>();
            int validMappings = 0;
            int totalMappings = 0;

            for (int i = 0; i < Math.Min(asciiRecords.Count, mb2000Records.Length); i++)
            {
                var asciiRecord = asciiRecords[i];
                var mb2000Fields = mb2000Records[i].Split('|');
                
                // Validate client number mapping
                var asciiClient = asciiRecord.Length > 3 ? asciiRecord.Substring(0, 3).Trim() : "";
                var mb2000Client = mb2000Fields.Length > 0 ? mb2000Fields[0] : "";
                
                totalMappings++;
                if (asciiClient == mb2000Client)
                {
                    validMappings++;
                }
                else
                {
                    issues.Add($"Client mapping mismatch at record {i}: ASCII='{asciiClient}', MB2000='{mb2000Client}'");
                }
            }

            var accuracy = totalMappings > 0 ? (validMappings * 100.0) / totalMappings : 0.0;
            return (accuracy, issues);
        }

        private (double accuracy, List<string> issues) CompareFieldAccuracy(string[] actualRecords, string[] expectedRecords)
        {
            var issues = new List<string>();
            int matchingFields = 0;
            int totalFields = 0;

            for (int i = 0; i < Math.Min(actualRecords.Length, expectedRecords.Length); i++)
            {
                var actualFields = actualRecords[i].Split('|');
                var expectedFields = expectedRecords[i].Split('|');
                
                for (int j = 0; j < Math.Min(actualFields.Length, expectedFields.Length); j++)
                {
                    totalFields++;
                    if (actualFields[j].Trim() == expectedFields[j].Trim())
                    {
                        matchingFields++;
                    }
                    else
                    {
                        issues.Add($"Field mismatch at record {i}, field {j}: Actual='{actualFields[j]}', Expected='{expectedFields[j]}'");
                    }
                }
            }

            var accuracy = totalFields > 0 ? (matchingFields * 100.0) / totalFields : 0.0;
            return (accuracy, issues);
        }
    }

    /// <summary>
    /// Comprehensive validation results for two-stage conversion
    /// </summary>
    public class TwoStageValidationResult
    {
        public DateTime ValidationStartTime { get; set; }
        public DateTime ValidationEndTime { get; set; }
        public bool Success { get; set; }
        public string Error { get; set; } = string.Empty;
        
        public StageValidationResult Stage1Validation { get; set; } = new();
        public StageValidationResult Stage2Validation { get; set; } = new();
        public StageValidationResult OverallValidation { get; set; } = new();
        
        public double OverallAccuracy { get; set; }
        
        public TimeSpan ValidationDuration => ValidationEndTime - ValidationStartTime;
    }

    /// <summary>
    /// Validation results for individual conversion stage
    /// </summary>
    public class StageValidationResult
    {
        public string StageName { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string Error { get; set; } = string.Empty;
        
        public int RecordCount { get; set; }
        public double AccuracyPercentage { get; set; }
        
        public double FormatAccuracy { get; set; }
        public double FieldAccuracy { get; set; }
        
        public List<string> FormatIssues { get; set; } = new();
        public List<string> FieldIssues { get; set; } = new();
    }
}
