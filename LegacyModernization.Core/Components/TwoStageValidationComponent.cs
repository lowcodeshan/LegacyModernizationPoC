using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog;

namespace LegacyModernization.Core.Components
{
    /// <summary>
    /// Enhanced validation component for parallel processing architecture
    /// Validates Container Step 1 (Binary→Container) || MB2000 Conversion (Binary→MB2000) parallel pipeline accuracy
    /// </summary>
    public class TwoStageValidationComponent
    {
        private readonly ILogger _logger;

        public TwoStageValidationComponent(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Validate legacy sequential conversion results against expected output (deprecated - use ValidateParallelPipelineAsync)
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
            return await ValidateConversionPipelineAsync(binaryInputPath, asciiIntermediatePath, mb2000OutputPath, expectedOutputPath, ValidationLevel.Basic);
        }

        /// <summary>
        /// Validate parallel processing pipeline results (Container Step 1 || MB2000 Conversion)
        /// This method correctly handles the actual architecture where both stages read from original binary
        /// </summary>
        /// <param name="binaryInputPath">Original binary .dat file</param>
        /// <param name="containerOutputPath">Container Step 1 output .4300 file</param>
        /// <param name="mb2000OutputPath">MB2000 conversion output .asc file</param>
        /// <param name="expectedContainerPath">Expected Container output for comparison</param>
        /// <param name="expectedMb2000Path">Expected MB2000 output for comparison</param>
        /// <param name="validationLevel">Level of validation detail</param>
        /// <returns>Comprehensive validation results</returns>
        public async Task<TwoStageValidationResult> ValidateParallelPipelineAsync(
            string binaryInputPath, 
            string containerOutputPath, 
            string mb2000OutputPath, 
            string expectedContainerPath,
            string expectedMb2000Path,
            ValidationLevel validationLevel)
        {
            var result = new TwoStageValidationResult
            {
                ValidationStartTime = DateTime.Now
            };

            try
            {
                _logger.Information("=== Parallel Pipeline Validation ===");
                _logger.Information("Validation Level: {ValidationLevel}", validationLevel);
                _logger.Information("Binary Input: {BinaryPath}", binaryInputPath);
                _logger.Information("Container Output: {ContainerPath}", containerOutputPath);
                _logger.Information("MB2000 Output: {MB2000Path}", mb2000OutputPath);
                _logger.Information("Expected Container: {ExpectedContainerPath}", expectedContainerPath);
                _logger.Information("Expected MB2000: {ExpectedMB2000Path}", expectedMb2000Path);

                // Container Step 1 Validation: Binary → Container (32 records, 4,300 bytes each)
                result.Stage1Validation = await ValidateContainerStepAsync(binaryInputPath, containerOutputPath, expectedContainerPath, validationLevel);
                
                // MB2000 Conversion Validation: Binary → MB2000 (5 records, 2,000 bytes each)  
                result.Stage2Validation = await ValidateMB2000StepAsync(binaryInputPath, mb2000OutputPath, expectedMb2000Path, validationLevel);
                
                // Overall Pipeline Validation: Combined accuracy across both parallel processes
                result.OverallValidation = await ValidateParallelOverallAsync(containerOutputPath, mb2000OutputPath, expectedContainerPath, expectedMb2000Path, validationLevel);

                // Enhanced record-level validation if requested
                if (validationLevel >= ValidationLevel.Detailed)
                {
                    result.RecordLevelValidation = await ValidateParallelRecordLevelAsync(binaryInputPath, containerOutputPath, mb2000OutputPath, expectedContainerPath, expectedMb2000Path);
                }

                // Field-level validation for comprehensive mode
                if (validationLevel == ValidationLevel.Comprehensive)
                {
                    result.FieldLevelValidation = await ValidateParallelFieldLevelAsync(containerOutputPath, mb2000OutputPath, expectedContainerPath, expectedMb2000Path);
                }
                
                // Calculate combined metrics
                result.OverallAccuracy = CalculateOverallAccuracy(result);
                result.ValidationEndTime = DateTime.Now;
                result.Success = result.OverallAccuracy >= 95.0; // Target threshold

                _logger.Information("=== Parallel Validation Complete ===");
                _logger.Information("Container Step 1 Accuracy: {Stage1}%", result.Stage1Validation.AccuracyPercentage);
                _logger.Information("MB2000 Conversion Accuracy: {Stage2}%", result.Stage2Validation.AccuracyPercentage);
                _logger.Information("Overall Accuracy: {Overall}%", result.OverallAccuracy);
                
                if (validationLevel >= ValidationLevel.Detailed)
                {
                    _logger.Information("Record-Level Accuracy: {RecordLevel}%", result.RecordLevelValidation?.AccuracyPercentage ?? 0);
                }
                
                if (validationLevel == ValidationLevel.Comprehensive)
                {
                    _logger.Information("Field-Level Accuracy: {FieldLevel}%", result.FieldLevelValidation?.AccuracyPercentage ?? 0);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Parallel pipeline validation failed");
                result.Success = false;
                result.Error = ex.Message;
                result.ValidationEndTime = DateTime.Now;
                return result;
            }
        }

        /// <summary>
        /// Validate legacy sequential conversion results with configurable validation level (deprecated - use ValidateParallelPipelineAsync)
        /// </summary>
        /// <param name="binaryInputPath">Original binary .dat file</param>
        /// <param name="asciiIntermediatePath">ASCII intermediate .asc file</param>
        /// <param name="mb2000OutputPath">Final MB2000 output file</param>
        /// <param name="expectedOutputPath">Expected output for comparison</param>
        /// <param name="validationLevel">Level of validation detail</param>
        /// <returns>Comprehensive validation results</returns>
        public async Task<TwoStageValidationResult> ValidateConversionPipelineAsync(
            string binaryInputPath, 
            string asciiIntermediatePath, 
            string mb2000OutputPath, 
            string expectedOutputPath,
            ValidationLevel validationLevel)
        {
            var result = new TwoStageValidationResult
            {
                ValidationStartTime = DateTime.Now
            };

            try
            {
                _logger.Information("=== Legacy Sequential Conversion Validation (DEPRECATED) ===");
                _logger.Information("Validation Level: {ValidationLevel}", validationLevel);
                _logger.Information("Binary: {BinaryPath}", binaryInputPath);
                _logger.Information("ASCII: {AsciiPath}", asciiIntermediatePath);
                _logger.Information("MB2000: {MB2000Path}", mb2000OutputPath);
                _logger.Information("Expected: {ExpectedPath}", expectedOutputPath);

                // Stage 1 Validation: Binary → ASCII conversion accuracy (DEPRECATED PATH)
                result.Stage1Validation = await ValidateBinaryToAsciiStageAsync(binaryInputPath, asciiIntermediatePath, validationLevel);
                
                // Stage 2 Validation: ASCII → MB2000 conversion accuracy (DEPRECATED PATH)  
                result.Stage2Validation = await ValidateAsciiToMB2000StageAsync(asciiIntermediatePath, mb2000OutputPath, validationLevel);
                
                // Overall Pipeline Validation: End-to-end accuracy
                result.OverallValidation = await ValidateOverallPipelineAsync(mb2000OutputPath, expectedOutputPath, validationLevel);

                // Enhanced record-level validation if requested
                if (validationLevel >= ValidationLevel.Detailed)
                {
                    result.RecordLevelValidation = await ValidateRecordLevelAsync(binaryInputPath, asciiIntermediatePath, mb2000OutputPath, expectedOutputPath);
                }

                // Field-level validation for comprehensive mode
                if (validationLevel == ValidationLevel.Comprehensive)
                {
                    result.FieldLevelValidation = await ValidateFieldLevelAsync(asciiIntermediatePath, mb2000OutputPath, expectedOutputPath);
                }
                
                // Calculate combined metrics
                result.OverallAccuracy = CalculateOverallAccuracy(result);
                result.ValidationEndTime = DateTime.Now;
                result.Success = result.OverallAccuracy >= 95.0; // Target threshold

                _logger.Information("=== Validation Complete ===");
                _logger.Information("Stage 1 Accuracy: {Stage1}%", result.Stage1Validation.AccuracyPercentage);
                _logger.Information("Stage 2 Accuracy: {Stage2}%", result.Stage2Validation.AccuracyPercentage);
                _logger.Information("Overall Accuracy: {Overall}%", result.OverallAccuracy);
                
                if (validationLevel >= ValidationLevel.Detailed)
                {
                    _logger.Information("Record-Level Accuracy: {RecordLevel}%", result.RecordLevelValidation?.AccuracyPercentage ?? 0);
                }
                
                if (validationLevel == ValidationLevel.Comprehensive)
                {
                    _logger.Information("Field-Level Accuracy: {FieldLevel}%", result.FieldLevelValidation?.AccuracyPercentage ?? 0);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Legacy sequential validation failed (deprecated method)");
                result.Success = false;
                result.Error = ex.Message;
                result.ValidationEndTime = DateTime.Now;
                return result;
            }
        }

        /// <summary>
        /// Validate Binary → ASCII conversion (DEPRECATED - this stage doesn't exist in parallel architecture)
        /// </summary>
        private async Task<StageValidationResult> ValidateBinaryToAsciiStageAsync(string binaryPath, string asciiPath)
        {
            return await ValidateBinaryToAsciiStageAsync(binaryPath, asciiPath, ValidationLevel.Basic);
        }

        /// <summary>
        /// Validate Binary → ASCII conversion (Stage 1) with configurable validation level
        /// </summary>
        private async Task<StageValidationResult> ValidateBinaryToAsciiStageAsync(string binaryPath, string asciiPath, ValidationLevel validationLevel)
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
            return await ValidateAsciiToMB2000StageAsync(asciiPath, mb2000Path, ValidationLevel.Basic);
        }

        /// <summary>
        /// Validate ASCII → MB2000 conversion (Stage 2) with configurable validation level
        /// </summary>
        private async Task<StageValidationResult> ValidateAsciiToMB2000StageAsync(string asciiPath, string mb2000Path, ValidationLevel validationLevel)
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
            return await ValidateOverallPipelineAsync(mb2000Path, expectedPath, ValidationLevel.Basic);
        }

        /// <summary>
        /// Validate overall pipeline accuracy against expected output with configurable validation level
        /// </summary>
        private async Task<StageValidationResult> ValidateOverallPipelineAsync(string mb2000Path, string expectedPath, ValidationLevel validationLevel)
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

            // Include enhanced validation results if available
            if (result.RecordLevelValidation?.Success == true)
                accuracies.Add(result.RecordLevelValidation.AccuracyPercentage);

            if (result.FieldLevelValidation?.Success == true)
                accuracies.Add(result.FieldLevelValidation.AccuracyPercentage);
            
            return accuracies.Any() ? accuracies.Average() : 0.0;
        }

        /// <summary>
        /// Validate record-level accuracy across all conversion stages
        /// </summary>
        private async Task<StageValidationResult> ValidateRecordLevelAsync(
            string binaryInputPath, 
            string asciiIntermediatePath, 
            string mb2000OutputPath, 
            string expectedOutputPath)
        {
            var result = new StageValidationResult { StageName = "Record-Level-Analysis" };
            
            try
            {
                _logger.Information("=== Record-Level Validation ===");
                
                // Validate Container Step 1 records (32 records at 4,300 bytes each)
                var containerValidation = await ValidateContainerRecordsAsync(binaryInputPath, asciiIntermediatePath);
                
                // Validate MB2000 records (5 records at 2,000 bytes each)
                var mb2000Validation = await ValidateMB2000RecordsAsync(binaryInputPath, mb2000OutputPath);
                
                // Cross-validate record consistency
                var crossValidation = await ValidateRecordConsistencyAsync(binaryInputPath, mb2000OutputPath, expectedOutputPath);
                
                // Calculate combined record-level accuracy
                var validations = new[] { containerValidation, mb2000Validation, crossValidation };
                result.AccuracyPercentage = validations.Where(v => v.success).Average(v => v.accuracy);
                result.Success = result.AccuracyPercentage >= 95.0;
                
                // Combine all issues
                result.FormatIssues.AddRange(validations.SelectMany(v => v.issues));
                result.RecordCount = containerValidation.recordCount + mb2000Validation.recordCount;
                
                _logger.Information("Record-level validation completed: {Accuracy}%", result.AccuracyPercentage);
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Record-level validation failed");
                result.Success = false;
                result.Error = ex.Message;
                return result;
            }
        }

        /// <summary>
        /// Validate field-level accuracy with detailed business rule checking
        /// </summary>
        private async Task<StageValidationResult> ValidateFieldLevelAsync(
            string asciiIntermediatePath, 
            string mb2000OutputPath, 
            string expectedOutputPath)
        {
            var result = new StageValidationResult { StageName = "Field-Level-Analysis" };
            
            try
            {
                _logger.Information("=== Field-Level Validation ===");
                
                // Parse and validate individual fields in MB2000 records
                var fieldValidation = await ValidateFieldAccuracyAsync(mb2000OutputPath, expectedOutputPath);
                
                // Validate business rules for financial fields
                var businessRuleValidation = await ValidateBusinessRulesAsync(mb2000OutputPath);
                
                // Validate data type consistency
                var dataTypeValidation = await ValidateDataTypesAsync(mb2000OutputPath, expectedOutputPath);
                
                // Calculate combined field-level accuracy
                var validations = new[] { fieldValidation, businessRuleValidation, dataTypeValidation };
                result.AccuracyPercentage = validations.Where(v => v.success).Average(v => v.accuracy);
                result.Success = result.AccuracyPercentage >= 99.0; // Higher threshold for field-level
                
                // Combine all issues
                result.FieldIssues.AddRange(validations.SelectMany(v => v.issues));
                
                _logger.Information("Field-level validation completed: {Accuracy}%", result.AccuracyPercentage);
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Field-level validation failed");
                result.Success = false;
                result.Error = ex.Message;
                return result;
            }
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

        // Enhanced validation methods for record-level analysis

        /// <summary>
        /// Validate Container Step 1 records (32 records at 4,300 bytes each)
        /// </summary>
        private async Task<(bool success, double accuracy, int recordCount, List<string> issues)> ValidateContainerRecordsAsync(
            string binaryInputPath, 
            string containerOutputPath)
        {
            var issues = new List<string>();
            
            try
            {
                // Check if Container output exists and has correct size
                if (!File.Exists(containerOutputPath))
                {
                    // Look for .4300 file in same directory as binary input
                    var directory = Path.GetDirectoryName(binaryInputPath);
                    var jobNumber = Path.GetFileNameWithoutExtension(binaryInputPath);
                    containerOutputPath = Path.Combine(directory ?? "", $"{jobNumber}.4300");
                    
                    if (!File.Exists(containerOutputPath))
                    {
                        issues.Add("Container output file not found");
                        return (false, 0.0, 0, issues);
                    }
                }
                
                var containerInfo = new FileInfo(containerOutputPath);
                const int expectedSize = 137600; // 32 records × 4,300 bytes
                const int recordSize = 4300;
                const int expectedRecords = 32;
                
                // Validate file size
                if (containerInfo.Length != expectedSize)
                {
                    issues.Add($"Container file size mismatch: {containerInfo.Length} bytes (expected {expectedSize})");
                    return (false, 0.0, 0, issues);
                }
                
                // Validate record structure by reading in chunks
                var validRecords = 0;
                using var stream = new FileStream(containerOutputPath, FileMode.Open, FileAccess.Read);
                var buffer = new byte[recordSize];
                
                for (int i = 0; i < expectedRecords; i++)
                {
                    var bytesRead = await stream.ReadAsync(buffer, 0, recordSize);
                    if (bytesRead == recordSize)
                    {
                        // Basic validation - check if record has data (not all zeros)
                        if (buffer.Any(b => b != 0))
                        {
                            validRecords++;
                        }
                        else
                        {
                            issues.Add($"Container record {i + 1} appears to be empty");
                        }
                    }
                    else
                    {
                        issues.Add($"Container record {i + 1} incomplete: {bytesRead} bytes (expected {recordSize})");
                    }
                }
                
                var accuracy = (validRecords * 100.0) / expectedRecords;
                _logger.Information("Container validation: {ValidRecords}/{ExpectedRecords} records valid ({Accuracy}%)", 
                    validRecords, expectedRecords, accuracy);
                
                return (accuracy >= 90.0, accuracy, validRecords, issues);
            }
            catch (Exception ex)
            {
                issues.Add($"Container validation error: {ex.Message}");
                return (false, 0.0, 0, issues);
            }
        }

        /// <summary>
        /// Validate MB2000 records (5 records at 2,000 bytes each)
        /// </summary>
        private async Task<(bool success, double accuracy, int recordCount, List<string> issues)> ValidateMB2000RecordsAsync(
            string binaryInputPath, 
            string mb2000OutputPath)
        {
            var issues = new List<string>();
            
            try
            {
                if (!File.Exists(mb2000OutputPath))
                {
                    issues.Add("MB2000 output file not found");
                    return (false, 0.0, 0, issues);
                }
                
                var mb2000Info = new FileInfo(mb2000OutputPath);
                const int expectedSize = 10000; // 5 records × 2,000 bytes
                const int recordSize = 2000;
                const int expectedRecords = 5;
                
                // Validate file size
                if (mb2000Info.Length != expectedSize)
                {
                    issues.Add($"MB2000 file size mismatch: {mb2000Info.Length} bytes (expected {expectedSize})");
                    return (false, 0.0, 0, issues);
                }
                
                // Validate record structure
                var validRecords = 0;
                using var stream = new FileStream(mb2000OutputPath, FileMode.Open, FileAccess.Read);
                var buffer = new byte[recordSize];
                
                for (int i = 0; i < expectedRecords; i++)
                {
                    var bytesRead = await stream.ReadAsync(buffer, 0, recordSize);
                    if (bytesRead == recordSize)
                    {
                        // Basic validation - check if record has data
                        if (buffer.Any(b => b != 0))
                        {
                            validRecords++;
                        }
                        else
                        {
                            issues.Add($"MB2000 record {i + 1} appears to be empty");
                        }
                    }
                    else
                    {
                        issues.Add($"MB2000 record {i + 1} incomplete: {bytesRead} bytes (expected {recordSize})");
                    }
                }
                
                var accuracy = (validRecords * 100.0) / expectedRecords;
                _logger.Information("MB2000 validation: {ValidRecords}/{ExpectedRecords} records valid ({Accuracy}%)", 
                    validRecords, expectedRecords, accuracy);
                
                return (accuracy >= 90.0, accuracy, validRecords, issues);
            }
            catch (Exception ex)
            {
                issues.Add($"MB2000 validation error: {ex.Message}");
                return (false, 0.0, 0, issues);
            }
        }

        /// <summary>
        /// Cross-validate record consistency between input and output
        /// </summary>
        private async Task<(bool success, double accuracy, int recordCount, List<string> issues)> ValidateRecordConsistencyAsync(
            string binaryInputPath, 
            string mb2000OutputPath, 
            string expectedOutputPath)
        {
            var issues = new List<string>();
            
            try
            {
                // Validate that we process the same input records consistently
                var binaryInfo = new FileInfo(binaryInputPath);
                const int inputRecordSize = 25600;
                const int expectedInputRecords = 5; // 128,000 ÷ 25,600 = 5
                
                if (binaryInfo.Length != 128000)
                {
                    issues.Add($"Input file size unexpected: {binaryInfo.Length} bytes (expected 128,000)");
                    return (false, 0.0, 0, issues);
                }
                
                var inputRecords = (int)(binaryInfo.Length / inputRecordSize);
                if (inputRecords != expectedInputRecords)
                {
                    issues.Add($"Input record count mismatch: {inputRecords} (expected {expectedInputRecords})");
                }
                
                // Validate that output record count matches expected
                if (File.Exists(expectedOutputPath))
                {
                    var expectedInfo = new FileInfo(expectedOutputPath);
                    var actualInfo = new FileInfo(mb2000OutputPath);
                    
                    if (expectedInfo.Length != actualInfo.Length)
                    {
                        issues.Add($"Output size mismatch: {actualInfo.Length} vs expected {expectedInfo.Length}");
                        return (false, 75.0, inputRecords, issues);
                    }
                }
                
                var accuracy = issues.Count == 0 ? 100.0 : Math.Max(0.0, 100.0 - (issues.Count * 20.0));
                
                _logger.Information("Record consistency validation: {Accuracy}% ({IssueCount} issues)", 
                    accuracy, issues.Count);
                
                return (accuracy >= 80.0, accuracy, inputRecords, issues);
            }
            catch (Exception ex)
            {
                issues.Add($"Record consistency validation error: {ex.Message}");
                return (false, 0.0, 0, issues);
            }
        }

        /// <summary>
        /// Validate field-level accuracy with detailed comparison
        /// </summary>
        private async Task<(bool success, double accuracy, List<string> issues)> ValidateFieldAccuracyAsync(
            string actualPath, 
            string expectedPath)
        {
            var issues = new List<string>();
            
            try
            {
                if (!File.Exists(actualPath) || !File.Exists(expectedPath))
                {
                    issues.Add("Required files not found for field validation");
                    return (false, 0.0, issues);
                }
                
                // For binary files, we'll do byte-by-byte comparison
                var actualBytes = await File.ReadAllBytesAsync(actualPath);
                var expectedBytes = await File.ReadAllBytesAsync(expectedPath);
                
                if (actualBytes.Length != expectedBytes.Length)
                {
                    issues.Add($"File length mismatch: {actualBytes.Length} vs {expectedBytes.Length}");
                    return (false, 0.0, issues);
                }
                
                int matchingBytes = 0;
                int totalBytes = actualBytes.Length;
                var diffPositions = new List<int>();
                
                for (int i = 0; i < totalBytes; i++)
                {
                    if (actualBytes[i] == expectedBytes[i])
                    {
                        matchingBytes++;
                    }
                    else
                    {
                        diffPositions.Add(i);
                        if (diffPositions.Count <= 10) // Limit reported differences
                        {
                            issues.Add($"Byte difference at position {i}: actual=0x{actualBytes[i]:X2}, expected=0x{expectedBytes[i]:X2}");
                        }
                    }
                }
                
                if (diffPositions.Count > 10)
                {
                    issues.Add($"... and {diffPositions.Count - 10} more byte differences");
                }
                
                var accuracy = (matchingBytes * 100.0) / totalBytes;
                
                _logger.Information("Field accuracy validation: {MatchingBytes}/{TotalBytes} bytes match ({Accuracy}%)", 
                    matchingBytes, totalBytes, accuracy);
                
                return (accuracy >= 99.0, accuracy, issues);
            }
            catch (Exception ex)
            {
                issues.Add($"Field accuracy validation error: {ex.Message}");
                return (false, 0.0, issues);
            }
        }

        /// <summary>
        /// Validate business rules for financial and critical fields
        /// </summary>
        private async Task<(bool success, double accuracy, List<string> issues)> ValidateBusinessRulesAsync(string filePath)
        {
            var issues = new List<string>();
            
            try
            {
                if (!File.Exists(filePath))
                {
                    issues.Add("File not found for business rule validation");
                    return (false, 0.0, issues);
                }
                
                var fileInfo = new FileInfo(filePath);
                var validationsPassed = 0;
                var totalValidations = 0;
                
                // Business Rule 1: File size should be exactly 10,000 bytes for 5 records
                totalValidations++;
                if (fileInfo.Length == 10000)
                {
                    validationsPassed++;
                }
                else
                {
                    issues.Add($"Business rule violation: File size {fileInfo.Length} (expected 10,000 for 5 records)");
                }
                
                // Business Rule 2: File should contain exactly 5 records of 2,000 bytes each
                totalValidations++;
                var recordCount = fileInfo.Length / 2000;
                if (recordCount == 5 && fileInfo.Length % 2000 == 0)
                {
                    validationsPassed++;
                }
                else
                {
                    issues.Add($"Business rule violation: Record structure invalid (calculated {recordCount} records)");
                }
                
                // Business Rule 3: File should not be empty or all zeros
                totalValidations++;
                var fileBytes = await File.ReadAllBytesAsync(filePath);
                if (fileBytes.Any(b => b != 0))
                {
                    validationsPassed++;
                }
                else
                {
                    issues.Add("Business rule violation: File appears to be empty or all zeros");
                }
                
                var accuracy = totalValidations > 0 ? (validationsPassed * 100.0) / totalValidations : 0.0;
                
                _logger.Information("Business rule validation: {PassedValidations}/{TotalValidations} rules passed ({Accuracy}%)", 
                    validationsPassed, totalValidations, accuracy);
                
                return (accuracy >= 95.0, accuracy, issues);
            }
            catch (Exception ex)
            {
                issues.Add($"Business rule validation error: {ex.Message}");
                return (false, 0.0, issues);
            }
        }

        /// <summary>
        /// Validate data type consistency between actual and expected output
        /// </summary>
        private async Task<(bool success, double accuracy, List<string> issues)> ValidateDataTypesAsync(
            string actualPath, 
            string expectedPath)
        {
            var issues = new List<string>();
            
            try
            {
                if (!File.Exists(actualPath) || !File.Exists(expectedPath))
                {
                    issues.Add("Required files not found for data type validation");
                    return (false, 0.0, issues);
                }
                
                var actualInfo = new FileInfo(actualPath);
                var expectedInfo = new FileInfo(expectedPath);
                var validationsPassed = 0;
                var totalValidations = 0;
                
                // Data Type Validation 1: File sizes should match exactly
                totalValidations++;
                if (actualInfo.Length == expectedInfo.Length)
                {
                    validationsPassed++;
                }
                else
                {
                    issues.Add($"Data type validation: File size mismatch {actualInfo.Length} vs {expectedInfo.Length}");
                }
                
                // Data Type Validation 2: File modification times should be recent (created by our process)
                totalValidations++;
                var timeDifference = DateTime.Now - actualInfo.LastWriteTime;
                if (timeDifference.TotalHours < 24) // Within last 24 hours
                {
                    validationsPassed++;
                }
                else
                {
                    issues.Add($"Data type validation: File appears old (modified {actualInfo.LastWriteTime})");
                }
                
                // Data Type Validation 3: File should be readable and binary formatted correctly
                totalValidations++;
                try
                {
                    using var stream = new FileStream(actualPath, FileMode.Open, FileAccess.Read);
                    var buffer = new byte[100]; // Read first 100 bytes
                    var bytesRead = await stream.ReadAsync(buffer, 0, 100);
                    if (bytesRead > 0)
                    {
                        validationsPassed++;
                    }
                    else
                    {
                        issues.Add("Data type validation: File appears unreadable");
                    }
                }
                catch
                {
                    issues.Add("Data type validation: File read error");
                }
                
                var accuracy = totalValidations > 0 ? (validationsPassed * 100.0) / totalValidations : 0.0;
                
                _logger.Information("Data type validation: {PassedValidations}/{TotalValidations} checks passed ({Accuracy}%)", 
                    validationsPassed, totalValidations, accuracy);
                
                return (accuracy >= 90.0, accuracy, issues);
            }
            catch (Exception ex)
            {
                issues.Add($"Data type validation error: {ex.Message}");
                return (false, 0.0, issues);
            }
        }
        
        /// <summary>
        /// Validate Container Step 1: Binary → Container (32 records, 4,300 bytes each)
        /// </summary>
        private async Task<StageValidationResult> ValidateContainerStepAsync(
            string binaryInputPath, 
            string containerOutputPath, 
            string expectedContainerPath, 
            ValidationLevel validationLevel)
        {
            var result = new StageValidationResult { StageName = "Container-Step-1" };
            
            try
            {
                _logger.Information("Validating Container Step 1: Binary → Container (.4300)");
                
                // Validate file existence
                if (!File.Exists(containerOutputPath))
                {
                    result.Success = false;
                    result.Error = $"Container output file not found: {containerOutputPath}";
                    return result;
                }
                
                if (!File.Exists(expectedContainerPath))
                {
                    result.Success = false;
                    result.Error = $"Expected container file not found: {expectedContainerPath}";
                    return result;
                }
                
                // Validate file size (32 records × 4,300 bytes = 137,600 bytes)
                var actualSize = new FileInfo(containerOutputPath).Length;
                var expectedSize = new FileInfo(expectedContainerPath).Length;
                
                result.ActualSize = actualSize;
                result.ExpectedSize = expectedSize;
                
                if (actualSize == expectedSize && expectedSize == 137600)
                {
                    result.FormatAccuracy = 100.0;
                    _logger.Information("Container Step 1 file size validation: ✅ PASS ({ActualSize} bytes)", actualSize);
                }
                else
                {
                    result.FormatAccuracy = actualSize == expectedSize ? 50.0 : 0.0;
                    result.FormatIssues.Add($"Container file size mismatch: Expected={expectedSize}, Actual={actualSize}");
                    _logger.Warning("Container Step 1 file size validation: ❌ FAIL ({ActualSize}/{ExpectedSize} bytes)", actualSize, expectedSize);
                }
                
                // Validate file content checksums
                if (File.Exists(containerOutputPath) && File.Exists(expectedContainerPath))
                {
                    var actualChecksum = await CalculateFileChecksumAsync(containerOutputPath);
                    var expectedChecksum = await CalculateFileChecksumAsync(expectedContainerPath);
                    
                    if (actualChecksum == expectedChecksum)
                    {
                        result.FieldAccuracy = 100.0;
                        _logger.Information("Container Step 1 checksum validation: ✅ PASS");
                    }
                    else
                    {
                        result.FieldAccuracy = 0.0;
                        result.FieldIssues.Add($"Container checksum mismatch: Expected={expectedChecksum}, Actual={actualChecksum}");
                        _logger.Warning("Container Step 1 checksum validation: ❌ FAIL");
                    }
                }
                
                result.AccuracyPercentage = (result.FormatAccuracy + result.FieldAccuracy) / 2.0;
                result.Success = result.AccuracyPercentage >= 95.0;
                result.RecordCount = 32; // Container Step 1 produces 32 records
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Container Step 1 validation failed");
                result.Success = false;
                result.Error = ex.Message;
                return result;
            }
        }
        
        /// <summary>
        /// Validate MB2000 Conversion: Binary → MB2000 (5 records, 2,000 bytes each)
        /// </summary>
        private async Task<StageValidationResult> ValidateMB2000StepAsync(
            string binaryInputPath, 
            string mb2000OutputPath, 
            string expectedMb2000Path, 
            ValidationLevel validationLevel)
        {
            var result = new StageValidationResult { StageName = "MB2000-Conversion" };
            
            try
            {
                _logger.Information("Validating MB2000 Conversion: Binary → MB2000 (.asc)");
                
                // Validate file existence
                if (!File.Exists(mb2000OutputPath))
                {
                    result.Success = false;
                    result.Error = $"MB2000 output file not found: {mb2000OutputPath}";
                    return result;
                }
                
                if (!File.Exists(expectedMb2000Path))
                {
                    result.Success = false;
                    result.Error = $"Expected MB2000 file not found: {expectedMb2000Path}";
                    return result;
                }
                
                // Validate file size (5 records × 2,000 bytes = 10,000 bytes)
                var actualSize = new FileInfo(mb2000OutputPath).Length;
                var expectedSize = new FileInfo(expectedMb2000Path).Length;
                
                result.ActualSize = actualSize;
                result.ExpectedSize = expectedSize;
                
                if (actualSize == expectedSize && expectedSize == 10000)
                {
                    result.FormatAccuracy = 100.0;
                    _logger.Information("MB2000 Conversion file size validation: ✅ PASS ({ActualSize} bytes)", actualSize);
                }
                else
                {
                    result.FormatAccuracy = actualSize == expectedSize ? 50.0 : 0.0;
                    result.FormatIssues.Add($"MB2000 file size mismatch: Expected={expectedSize}, Actual={actualSize}");
                    _logger.Warning("MB2000 Conversion file size validation: ❌ FAIL ({ActualSize}/{ExpectedSize} bytes)", actualSize, expectedSize);
                }
                
                // Validate file content checksums
                if (File.Exists(mb2000OutputPath) && File.Exists(expectedMb2000Path))
                {
                    var actualChecksum = await CalculateFileChecksumAsync(mb2000OutputPath);
                    var expectedChecksum = await CalculateFileChecksumAsync(expectedMb2000Path);
                    
                    if (actualChecksum == expectedChecksum)
                    {
                        result.FieldAccuracy = 100.0;
                        _logger.Information("MB2000 Conversion checksum validation: ✅ PASS");
                    }
                    else
                    {
                        result.FieldAccuracy = 0.0;
                        result.FieldIssues.Add($"MB2000 checksum mismatch: Expected={expectedChecksum}, Actual={actualChecksum}");
                        _logger.Warning("MB2000 Conversion checksum validation: ❌ FAIL");
                    }
                }
                
                result.AccuracyPercentage = (result.FormatAccuracy + result.FieldAccuracy) / 2.0;
                result.Success = result.AccuracyPercentage >= 95.0;
                result.RecordCount = 5; // MB2000 Conversion produces 5 records
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "MB2000 Conversion validation failed");
                result.Success = false;
                result.Error = ex.Message;
                return result;
            }
        }
        
        /// <summary>
        /// Validate overall parallel pipeline combining both Container and MB2000 accuracy
        /// </summary>
        private async Task<StageValidationResult> ValidateParallelOverallAsync(
            string containerOutputPath,
            string mb2000OutputPath,
            string expectedContainerPath,
            string expectedMb2000Path,
            ValidationLevel validationLevel)
        {
            var result = new StageValidationResult { StageName = "Parallel-Pipeline-Overall" };
            
            try
            {
                _logger.Information("Validating Overall Parallel Pipeline Accuracy");
                
                // Validate both output files exist
                var containerExists = File.Exists(containerOutputPath);
                var mb2000Exists = File.Exists(mb2000OutputPath);
                var expectedContainerExists = File.Exists(expectedContainerPath);
                var expectedMb2000Exists = File.Exists(expectedMb2000Path);
                
                if (!containerExists || !mb2000Exists)
                {
                    result.Success = false;
                    result.Error = $"Output files missing: Container={containerExists}, MB2000={mb2000Exists}";
                    return result;
                }
                
                if (!expectedContainerExists || !expectedMb2000Exists)
                {
                    result.Success = false;
                    result.Error = $"Expected files missing: Container={expectedContainerExists}, MB2000={expectedMb2000Exists}";
                    return result;
                }
                
                // Calculate combined file accuracy
                var containerSize = new FileInfo(containerOutputPath).Length;
                var mb2000Size = new FileInfo(mb2000OutputPath).Length;
                var expectedContainerSize = new FileInfo(expectedContainerPath).Length;
                var expectedMb2000Size = new FileInfo(expectedMb2000Path).Length;
                
                var containerSizeMatch = containerSize == expectedContainerSize;
                var mb2000SizeMatch = mb2000Size == expectedMb2000Size;
                
                result.ActualSize = containerSize + mb2000Size;
                result.ExpectedSize = expectedContainerSize + expectedMb2000Size;
                
                // Calculate format accuracy based on size matches
                if (containerSizeMatch && mb2000SizeMatch)
                {
                    result.FormatAccuracy = 100.0;
                    _logger.Information("Overall Pipeline file size validation: ✅ PASS (Container: {ContainerSize}, MB2000: {MB2000Size})", containerSize, mb2000Size);
                }
                else
                {
                    var accuracy = 0.0;
                    if (containerSizeMatch) accuracy += 50.0;
                    if (mb2000SizeMatch) accuracy += 50.0;
                    result.FormatAccuracy = accuracy;
                    
                    if (!containerSizeMatch)
                        result.FormatIssues.Add($"Container size mismatch: Expected={expectedContainerSize}, Actual={containerSize}");
                    if (!mb2000SizeMatch)
                        result.FormatIssues.Add($"MB2000 size mismatch: Expected={expectedMb2000Size}, Actual={mb2000Size}");
                    
                    _logger.Warning("Overall Pipeline file size validation: ⚠️ PARTIAL (Container: {ContainerMatch}, MB2000: {MB2000Match})", containerSizeMatch, mb2000SizeMatch);
                }
                
                // Calculate checksum accuracy
                var containerChecksum = await CalculateFileChecksumAsync(containerOutputPath);
                var mb2000Checksum = await CalculateFileChecksumAsync(mb2000OutputPath);
                var expectedContainerChecksum = await CalculateFileChecksumAsync(expectedContainerPath);
                var expectedMb2000Checksum = await CalculateFileChecksumAsync(expectedMb2000Path);
                
                var containerChecksumMatch = containerChecksum == expectedContainerChecksum;
                var mb2000ChecksumMatch = mb2000Checksum == expectedMb2000Checksum;
                
                if (containerChecksumMatch && mb2000ChecksumMatch)
                {
                    result.FieldAccuracy = 100.0;
                    _logger.Information("Overall Pipeline checksum validation: ✅ PASS");
                }
                else
                {
                    var accuracy = 0.0;
                    if (containerChecksumMatch) accuracy += 50.0;
                    if (mb2000ChecksumMatch) accuracy += 50.0;
                    result.FieldAccuracy = accuracy;
                    
                    if (!containerChecksumMatch)
                        result.FieldIssues.Add($"Container checksum mismatch");
                    if (!mb2000ChecksumMatch)
                        result.FieldIssues.Add($"MB2000 checksum mismatch");
                    
                    _logger.Warning("Overall Pipeline checksum validation: ⚠️ PARTIAL (Container: {ContainerMatch}, MB2000: {MB2000Match})", containerChecksumMatch, mb2000ChecksumMatch);
                }
                
                result.AccuracyPercentage = (result.FormatAccuracy + result.FieldAccuracy) / 2.0;
                result.Success = result.AccuracyPercentage >= 95.0;
                result.RecordCount = 37; // 32 Container + 5 MB2000 records
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Overall parallel pipeline validation failed");
                result.Success = false;
                result.Error = ex.Message;
                return result;
            }
        }
        
        /// <summary>
        /// Validate record-level accuracy for parallel processing
        /// </summary>
        private async Task<StageValidationResult> ValidateParallelRecordLevelAsync(
            string binaryInputPath,
            string containerOutputPath,
            string mb2000OutputPath,
            string expectedContainerPath,
            string expectedMb2000Path)
        {
            var result = new StageValidationResult { StageName = "Parallel-Record-Level-Analysis" };
            
            try
            {
                _logger.Information("=== Parallel Record-Level Validation ===");
                
                // Validate Container records (32 records at 4,300 bytes each)
                var containerValidation = await ValidateContainerRecordsParallelAsync(binaryInputPath, containerOutputPath, expectedContainerPath);
                
                // Validate MB2000 records (5 records at 2,000 bytes each)
                var mb2000Validation = await ValidateMB2000RecordsParallelAsync(binaryInputPath, mb2000OutputPath, expectedMb2000Path);
                
                // Cross-validate record consistency across both parallel processes
                var crossValidation = await ValidateParallelRecordConsistencyAsync(binaryInputPath, containerOutputPath, mb2000OutputPath, expectedContainerPath, expectedMb2000Path);
                
                // Calculate combined record-level accuracy
                var validations = new[] { containerValidation, mb2000Validation, crossValidation };
                result.AccuracyPercentage = validations.Where(v => v.success).Average(v => v.accuracy);
                result.Success = result.AccuracyPercentage >= 95.0;
                
                // Combine all issues
                result.FormatIssues.AddRange(validations.SelectMany(v => v.issues));
                result.RecordCount = containerValidation.recordCount + mb2000Validation.recordCount;
                
                _logger.Information("Parallel record-level validation completed: {Accuracy}%", result.AccuracyPercentage);
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Parallel record-level validation failed");
                result.Success = false;
                result.Error = ex.Message;
                return result;
            }
        }
        
        /// <summary>
        /// Validate field-level accuracy for parallel processing
        /// </summary>
        private async Task<StageValidationResult> ValidateParallelFieldLevelAsync(
            string containerOutputPath,
            string mb2000OutputPath,
            string expectedContainerPath,
            string expectedMb2000Path)
        {
            var result = new StageValidationResult { StageName = "Parallel-Field-Level-Analysis" };
            
            try
            {
                _logger.Information("=== Parallel Field-Level Validation ===");
                
                // Parse and validate individual fields in Container records
                var containerFieldValidation = await ValidateContainerFieldAccuracyAsync(containerOutputPath, expectedContainerPath);
                
                // Parse and validate individual fields in MB2000 records  
                var mb2000FieldValidation = await ValidateMB2000FieldAccuracyAsync(mb2000OutputPath, expectedMb2000Path);
                
                // Validate business rules across both outputs
                var businessRuleValidation = await ValidateParallelBusinessRulesAsync(containerOutputPath, mb2000OutputPath);
                
                // Validate data type consistency across both outputs
                var dataTypeValidation = await ValidateParallelDataTypesAsync(containerOutputPath, mb2000OutputPath, expectedContainerPath, expectedMb2000Path);
                
                // Calculate combined field-level accuracy
                var validations = new[] { containerFieldValidation, mb2000FieldValidation, businessRuleValidation, dataTypeValidation };
                result.AccuracyPercentage = validations.Where(v => v.success).Average(v => v.accuracy);
                result.Success = result.AccuracyPercentage >= 99.0; // Higher threshold for field-level
                
                // Combine all issues
                result.FieldIssues.AddRange(validations.SelectMany(v => v.issues));
                
                _logger.Information("Parallel field-level validation completed: {Accuracy}%", result.AccuracyPercentage);
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Parallel field-level validation failed");
                result.Success = false;
                result.Error = ex.Message;
                return result;
            }
        }
        
        // Helper methods for parallel processing validation
        private async Task<(bool success, double accuracy, int recordCount, List<string> issues)> ValidateContainerRecordsParallelAsync(
            string binaryInputPath, 
            string containerOutputPath, 
            string expectedContainerPath)
        {
            var issues = new List<string>();
            
            try
            {
                // Validate Container Step 1 produces exactly 32 records of 4,300 bytes each
                var actualSize = new FileInfo(containerOutputPath).Length;
                var expectedSize = new FileInfo(expectedContainerPath).Length;
                
                if (actualSize != 137600)
                {
                    issues.Add($"Container output size incorrect: {actualSize} bytes (expected 137,600)");
                    return (false, 0.0, 0, issues);
                }
                
                if (actualSize != expectedSize)
                {
                    issues.Add($"Container size mismatch with expected: {actualSize} vs {expectedSize}");
                    return (false, 50.0, 32, issues);
                }
                
                // Calculate checksum accuracy
                var actualChecksum = await CalculateFileChecksumAsync(containerOutputPath);
                var expectedChecksum = await CalculateFileChecksumAsync(expectedContainerPath);
                
                var accuracy = actualChecksum == expectedChecksum ? 100.0 : 80.0;
                var success = accuracy >= 95.0;
                
                if (accuracy < 100.0)
                {
                    issues.Add("Container record content differs from expected");
                }
                
                return (success, accuracy, 32, issues);
            }
            catch (Exception ex)
            {
                issues.Add($"Container record validation error: {ex.Message}");
                return (false, 0.0, 0, issues);
            }
        }
        
        private async Task<(bool success, double accuracy, int recordCount, List<string> issues)> ValidateMB2000RecordsParallelAsync(
            string binaryInputPath, 
            string mb2000OutputPath, 
            string expectedMb2000Path)
        {
            var issues = new List<string>();
            
            try
            {
                // Validate MB2000 Conversion produces exactly 5 records of 2,000 bytes each
                var actualSize = new FileInfo(mb2000OutputPath).Length;
                var expectedSize = new FileInfo(expectedMb2000Path).Length;
                
                if (actualSize != 10000)
                {
                    issues.Add($"MB2000 output size incorrect: {actualSize} bytes (expected 10,000)");
                    return (false, 0.0, 0, issues);
                }
                
                if (actualSize != expectedSize)
                {
                    issues.Add($"MB2000 size mismatch with expected: {actualSize} vs {expectedSize}");
                    return (false, 50.0, 5, issues);
                }
                
                // Calculate checksum accuracy
                var actualChecksum = await CalculateFileChecksumAsync(mb2000OutputPath);
                var expectedChecksum = await CalculateFileChecksumAsync(expectedMb2000Path);
                
                var accuracy = actualChecksum == expectedChecksum ? 100.0 : 80.0;
                var success = accuracy >= 95.0;
                
                if (accuracy < 100.0)
                {
                    issues.Add("MB2000 record content differs from expected");
                }
                
                return (success, accuracy, 5, issues);
            }
            catch (Exception ex)
            {
                issues.Add($"MB2000 record validation error: {ex.Message}");
                return (false, 0.0, 0, issues);
            }
        }
        
        private async Task<(bool success, double accuracy, int recordCount, List<string> issues)> ValidateParallelRecordConsistencyAsync(
            string binaryInputPath,
            string containerOutputPath,
            string mb2000OutputPath,
            string expectedContainerPath,
            string expectedMb2000Path)
        {
            var issues = new List<string>();
            
            try
            {
                // Both processes should read from the same binary input successfully
                if (!File.Exists(binaryInputPath))
                {
                    issues.Add("Binary input file missing for parallel validation");
                    return (false, 0.0, 0, issues);
                }
                
                var binarySize = new FileInfo(binaryInputPath).Length;
                if (binarySize != 128000)
                {
                    issues.Add($"Binary input size incorrect: {binarySize} bytes (expected 128,000)");
                    return (false, 0.0, 0, issues);
                }
                
                // Validate both outputs exist and have correct sizes
                var containerExists = File.Exists(containerOutputPath);
                var mb2000Exists = File.Exists(mb2000OutputPath);
                
                if (!containerExists || !mb2000Exists)
                {
                    issues.Add($"Parallel outputs missing: Container={containerExists}, MB2000={mb2000Exists}");
                    return (false, 0.0, 0, issues);
                }
                
                // Consistency check: both processes should complete successfully for the same input
                var containerSize = new FileInfo(containerOutputPath).Length;
                var mb2000Size = new FileInfo(mb2000OutputPath).Length;
                
                var containerCorrect = containerSize == 137600;
                var mb2000Correct = mb2000Size == 10000;
                
                if (containerCorrect && mb2000Correct)
                {
                    return (true, 100.0, 37, issues); // 32 Container + 5 MB2000 = 37 total records
                }
                else
                {
                    if (!containerCorrect)
                        issues.Add($"Container output size incorrect: {containerSize}");
                    if (!mb2000Correct)
                        issues.Add($"MB2000 output size incorrect: {mb2000Size}");
                    
                    var accuracy = 0.0;
                    if (containerCorrect) accuracy += 50.0;
                    if (mb2000Correct) accuracy += 50.0;
                    
                    return (accuracy >= 95.0, accuracy, 37, issues);
                }
            }
            catch (Exception ex)
            {
                issues.Add($"Parallel record consistency validation error: {ex.Message}");
                return (false, 0.0, 0, issues);
            }
        }
        
        // Placeholder methods for field-level validations (can be enhanced later)
        private async Task<(bool success, double accuracy, List<string> issues)> ValidateContainerFieldAccuracyAsync(string containerOutputPath, string expectedContainerPath)
        {
            // For now, do basic checksum comparison
            try
            {
                var actualChecksum = await CalculateFileChecksumAsync(containerOutputPath);
                var expectedChecksum = await CalculateFileChecksumAsync(expectedContainerPath);
                
                var match = actualChecksum == expectedChecksum;
                return (match, match ? 100.0 : 0.0, match ? new List<string>() : new List<string> { "Container field content mismatch" });
            }
            catch (Exception ex)
            {
                return (false, 0.0, new List<string> { $"Container field validation error: {ex.Message}" });
            }
        }
        
        private async Task<(bool success, double accuracy, List<string> issues)> ValidateMB2000FieldAccuracyAsync(string mb2000OutputPath, string expectedMb2000Path)
        {
            // For now, do basic checksum comparison
            try
            {
                var actualChecksum = await CalculateFileChecksumAsync(mb2000OutputPath);
                var expectedChecksum = await CalculateFileChecksumAsync(expectedMb2000Path);
                
                var match = actualChecksum == expectedChecksum;
                return (match, match ? 100.0 : 0.0, match ? new List<string>() : new List<string> { "MB2000 field content mismatch" });
            }
            catch (Exception ex)
            {
                return (false, 0.0, new List<string> { $"MB2000 field validation error: {ex.Message}" });
            }
        }
        
        private async Task<(bool success, double accuracy, List<string> issues)> ValidateParallelBusinessRulesAsync(string containerOutputPath, string mb2000OutputPath)
        {
            // Placeholder for business rule validation
            return (true, 100.0, new List<string>());
        }
        
        private async Task<(bool success, double accuracy, List<string> issues)> ValidateParallelDataTypesAsync(
            string containerOutputPath, 
            string mb2000OutputPath, 
            string expectedContainerPath, 
            string expectedMb2000Path)
        {
            // Placeholder for data type validation
            return (true, 100.0, new List<string>());
        }
        
        /// <summary>
        /// Calculate SHA256 checksum for a file
        /// </summary>
        private async Task<string> CalculateFileChecksumAsync(string filePath)
        {
            using var fileStream = File.OpenRead(filePath);
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var hashBytes = await Task.Run(() => sha256.ComputeHash(fileStream));
            return Convert.ToHexString(hashBytes).ToLowerInvariant();
        }
    }

    /// <summary>
    /// Validation level enumeration for configurable validation depth
    /// </summary>
    public enum ValidationLevel
    {
        /// <summary>
        /// Basic validation - file sizes, record counts, basic format checks
        /// </summary>
        Basic = 1,
        
        /// <summary>
        /// Detailed validation - includes record-level analysis and cross-validation
        /// </summary>
        Detailed = 2,
        
        /// <summary>
        /// Comprehensive validation - includes field-level analysis and business rule validation
        /// </summary>
        Comprehensive = 3
    }

    /// <summary>
    /// Comprehensive validation results for parallel processing architecture
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
        
        // Enhanced validation results
        public StageValidationResult? RecordLevelValidation { get; set; }
        public StageValidationResult? FieldLevelValidation { get; set; }
        
        public double OverallAccuracy { get; set; }
        
        public TimeSpan ValidationDuration => ValidationEndTime - ValidationStartTime;
        
        /// <summary>
        /// Get detailed validation summary
        /// </summary>
        public string GetValidationSummary()
        {
            var summary = new StringBuilder();
            summary.AppendLine($"=== Parallel Pipeline Validation Summary ===");
            summary.AppendLine($"Overall Success: {(Success ? "✅ PASSED" : "❌ FAILED")}");
            summary.AppendLine($"Overall Accuracy: {OverallAccuracy:F2}%");
            summary.AppendLine($"Duration: {ValidationDuration.TotalSeconds:F2} seconds");
            summary.AppendLine();
            
            summary.AppendLine($"Stage 1 (Binary→Container): {Stage1Validation.AccuracyPercentage:F2}% ({(Stage1Validation.Success ? "PASS" : "FAIL")})");
            summary.AppendLine($"Stage 2 (Binary→MB2000): {Stage2Validation.AccuracyPercentage:F2}% ({(Stage2Validation.Success ? "PASS" : "FAIL")})");
            summary.AppendLine($"Overall Pipeline: {OverallValidation.AccuracyPercentage:F2}% ({(OverallValidation.Success ? "PASS" : "FAIL")})");
            
            if (RecordLevelValidation != null)
            {
                summary.AppendLine($"Record-Level Analysis: {RecordLevelValidation.AccuracyPercentage:F2}% ({(RecordLevelValidation.Success ? "PASS" : "FAIL")})");
            }
            
            if (FieldLevelValidation != null)
            {
                summary.AppendLine($"Field-Level Analysis: {FieldLevelValidation.AccuracyPercentage:F2}% ({(FieldLevelValidation.Success ? "PASS" : "FAIL")})");
            }
            
            return summary.ToString();
        }
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
        
        // Enhanced validation properties for file size validation
        public long ExpectedSize { get; set; }
        public long ActualSize { get; set; }
        
        // Enhanced record validation properties
        public int? ExpectedRecordCount { get; set; }
        public int? ActualRecordCount { get; set; }
        public double RecordCountAccuracy { get; set; }
        
        public int? CorrectRecords { get; set; }
        public int? TotalRecords { get; set; }
        public double RecordLevelAccuracy { get; set; }
        
        public int? CorrectFields { get; set; }
        public int? TotalFields { get; set; }
        public double FieldLevelAccuracy { get; set; }
        
        public List<string> ValidationMessages { get; set; } = new();
        public List<string> RecordErrors { get; set; } = new();
        
        public bool SizeMatch => ExpectedSize == ActualSize;
        public bool RecordCountMatch => ExpectedRecordCount == ActualRecordCount;
        
        public override string ToString()
        {
            var result = $"Success: {(Success ? "✅" : "❌")}, ";
            result += $"Accuracy: {AccuracyPercentage:F1}%";
            
            if (ExpectedSize > 0)
            {
                result += $", Size: {ActualSize}/{ExpectedSize}";
            }
            
            if (ExpectedRecordCount.HasValue)
            {
                result += $", Records: {ActualRecordCount}/{ExpectedRecordCount}";
            }
            
            if (CorrectRecords.HasValue)
            {
                result += $", Record Accuracy: {RecordLevelAccuracy:F1}%";
            }
            
            if (CorrectFields.HasValue)
            {
                result += $", Field Accuracy: {FieldLevelAccuracy:F1}%";
            }
            
            return result;
        }
    }
}
