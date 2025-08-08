using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Serilog;
using LegacyModernization.Core.Configuration;
using LegacyModernization.Core.Models;
using LegacyModernization.Core.Utilities;

namespace LegacyModernization.Core.Validation
{
    /// <summary>
    /// Comprehensive Output Validation Component
    /// Validates that C# implementation produces identical output to legacy system
    /// Task 3.2 - Agent_QA: Comprehensive Output Validation & Testing
    /// </summary>
    public class OutputValidator
    {
        private readonly ILogger _logger;
        private readonly PipelineConfiguration _configuration;

        public OutputValidator(ILogger logger, PipelineConfiguration configuration)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        /// <summary>
        /// Execute comprehensive output validation
        /// </summary>
        /// <param name="jobNumber">Job number to validate</param>
        /// <param name="expectedOutputPath">Path to expected output files</param>
        /// <returns>Validation result</returns>
        public async Task<ValidationResult> ValidateOutputAsync(string jobNumber, string expectedOutputPath)
        {
            var result = new ValidationResult
            {
                JobNumber = jobNumber,
                ValidationStartTime = DateTime.Now,
                ExpectedOutputPath = expectedOutputPath,
                ActualOutputPath = _configuration.OutputPath
            };

            try
            {
                _logger.Information("Starting comprehensive output validation for job {JobNumber}", jobNumber);

                // Validate all core output files
                var fileValidations = await ValidateAllOutputFilesAsync(jobNumber, expectedOutputPath);
                result.FileValidations.AddRange(fileValidations);

                // Calculate overall success
                result.Success = fileValidations.All(v => v.IsValid);
                result.TotalFilesValidated = fileValidations.Count;
                result.FilesMatched = fileValidations.Count(v => v.IsValid);
                result.FilesMismatched = fileValidations.Count(v => !v.IsValid);

                result.ValidationEndTime = DateTime.Now;

                if (result.Success)
                {
                    _logger.Information("All output files validated successfully for job {JobNumber}: {FilesMatched}/{TotalFiles} files matched", 
                        jobNumber, result.FilesMatched, result.TotalFilesValidated);
                }
                else
                {
                    _logger.Warning("Output validation found mismatches for job {JobNumber}: {FilesMatched}/{TotalFiles} files matched", 
                        jobNumber, result.FilesMatched, result.TotalFilesValidated);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Output validation failed for job {JobNumber}", jobNumber);
                result.Success = false;
                result.ErrorMessage = ex.Message;
                result.ValidationEndTime = DateTime.Now;
                return result;
            }
        }

        /// <summary>
        /// Validate all expected output files
        /// </summary>
        /// <param name="jobNumber">Job number</param>
        /// <param name="expectedOutputPath">Expected output path</param>
        /// <returns>List of file validation results</returns>
        private async Task<List<FileValidationResult>> ValidateAllOutputFilesAsync(string jobNumber, string expectedOutputPath)
        {
            var validations = new List<FileValidationResult>();

            // Core output files to validate
            var filesToValidate = new[]
            {
                $"{jobNumber}p.asc",      // Paper bills (main output)
                $"{jobNumber}e.txt",      // Electronic bills
                $"{jobNumber}e.asc",      // Electronic ASC
                $"{jobNumber}p.asc.org",  // Original backup
                $"{jobNumber}.se1",       // Supplemental file
                $"{jobNumber}.4300"       // Work2 file
            };

            foreach (var fileName in filesToValidate)
            {
                var validation = await ValidateOutputFileAsync(fileName, expectedOutputPath);
                validations.Add(validation);
            }

            return validations;
        }

        /// <summary>
        /// Validate a specific output file against expected output
        /// </summary>
        /// <param name="fileName">File name to validate</param>
        /// <param name="expectedOutputPath">Expected output path</param>
        /// <returns>File validation result</returns>
        private async Task<FileValidationResult> ValidateOutputFileAsync(string fileName, string expectedOutputPath)
        {
            var result = new FileValidationResult
            {
                FileName = fileName,
                ValidationStartTime = DateTime.Now
            };

            try
            {
                var actualFilePath = Path.Combine(_configuration.OutputPath, fileName);
                var expectedFilePath = Path.Combine(expectedOutputPath, fileName);

                result.ActualFilePath = actualFilePath;
                result.ExpectedFilePath = expectedFilePath;

                // Check if files exist
                var actualExists = File.Exists(actualFilePath);
                var expectedExists = File.Exists(expectedFilePath);

                if (!actualExists && !expectedExists)
                {
                    result.IsValid = true;
                    result.ValidationMessage = "Both files do not exist (expected)";
                    _logger.Debug("File validation passed: {FileName} - both files do not exist", fileName);
                    return result;
                }

                if (!actualExists)
                {
                    result.IsValid = false;
                    result.ValidationMessage = "Actual file does not exist";
                    _logger.Warning("File validation failed: {FileName} - actual file missing", fileName);
                    return result;
                }

                if (!expectedExists)
                {
                    result.IsValid = false;
                    result.ValidationMessage = "Expected file does not exist";
                    _logger.Warning("File validation failed: {FileName} - expected file missing", fileName);
                    return result;
                }

                // Compare file sizes
                var actualInfo = new FileInfo(actualFilePath);
                var expectedInfo = new FileInfo(expectedFilePath);

                result.ActualFileSize = actualInfo.Length;
                result.ExpectedFileSize = expectedInfo.Length;

                if (actualInfo.Length != expectedInfo.Length)
                {
                    result.IsValid = false;
                    result.ValidationMessage = $"File size mismatch: actual={actualInfo.Length}, expected={expectedInfo.Length}";
                    _logger.Warning("File validation failed: {FileName} - size mismatch: actual={ActualSize}, expected={ExpectedSize}", 
                        fileName, actualInfo.Length, expectedInfo.Length);
                    return result;
                }

                // Compare file content (binary comparison)
                var contentMatch = await CompareFileContentAsync(actualFilePath, expectedFilePath);
                
                if (contentMatch.IsIdentical)
                {
                    result.IsValid = true;
                    result.ValidationMessage = "Files match exactly";
                    result.ActualFileHash = contentMatch.ActualHash;
                    result.ExpectedFileHash = contentMatch.ExpectedHash;
                    _logger.Information("File validation passed: {FileName} - files match exactly ({FileSize} bytes)", 
                        fileName, actualInfo.Length);
                }
                else
                {
                    result.IsValid = false;
                    result.ValidationMessage = $"Content mismatch: {contentMatch.DifferenceDescription}";
                    result.ActualFileHash = contentMatch.ActualHash;
                    result.ExpectedFileHash = contentMatch.ExpectedHash;
                    result.FirstDifferenceOffset = contentMatch.FirstDifferenceOffset;
                    _logger.Warning("File validation failed: {FileName} - content mismatch: {Difference}", 
                        fileName, contentMatch.DifferenceDescription);
                }

                result.ValidationEndTime = DateTime.Now;
                return result;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "File validation error for {FileName}", fileName);
                result.IsValid = false;
                result.ValidationMessage = $"Validation error: {ex.Message}";
                result.ValidationEndTime = DateTime.Now;
                return result;
            }
        }

        /// <summary>
        /// Compare file content byte-by-byte
        /// </summary>
        /// <param name="actualFilePath">Actual file path</param>
        /// <param name="expectedFilePath">Expected file path</param>
        /// <returns>Content comparison result</returns>
        private async Task<ContentComparisonResult> CompareFileContentAsync(string actualFilePath, string expectedFilePath)
        {
            var result = new ContentComparisonResult();

            try
            {
                // Calculate file hashes for quick comparison
                result.ActualHash = await CalculateFileHashAsync(actualFilePath);
                result.ExpectedHash = await CalculateFileHashAsync(expectedFilePath);

                if (result.ActualHash == result.ExpectedHash)
                {
                    result.IsIdentical = true;
                    return result;
                }

                // Files differ - find first difference
                using var actualStream = new FileStream(actualFilePath, FileMode.Open, FileAccess.Read);
                using var expectedStream = new FileStream(expectedFilePath, FileMode.Open, FileAccess.Read);

                var buffer1 = new byte[4096];
                var buffer2 = new byte[4096];
                long offset = 0;

                while (true)
                {
                    var bytesRead1 = await actualStream.ReadAsync(buffer1, 0, buffer1.Length);
                    var bytesRead2 = await expectedStream.ReadAsync(buffer2, 0, buffer2.Length);

                    if (bytesRead1 != bytesRead2)
                    {
                        result.DifferenceDescription = $"Different read lengths at offset {offset}: actual={bytesRead1}, expected={bytesRead2}";
                        result.FirstDifferenceOffset = offset;
                        break;
                    }

                    if (bytesRead1 == 0)
                    {
                        // End of both files - should not reach here since hashes differ
                        result.DifferenceDescription = "Hash mismatch but no byte differences found";
                        break;
                    }

                    for (int i = 0; i < bytesRead1; i++)
                    {
                        if (buffer1[i] != buffer2[i])
                        {
                            result.DifferenceDescription = $"Byte difference at offset {offset + i}: actual=0x{buffer1[i]:X2}, expected=0x{buffer2[i]:X2}";
                            result.FirstDifferenceOffset = offset + i;
                            return result;
                        }
                    }

                    offset += bytesRead1;
                }

                return result;
            }
            catch (Exception ex)
            {
                result.IsIdentical = false;
                result.DifferenceDescription = $"Comparison error: {ex.Message}";
                return result;
            }
        }

        /// <summary>
        /// Calculate SHA256 hash of a file
        /// </summary>
        /// <param name="filePath">File path</param>
        /// <returns>Hash string</returns>
        private async Task<string> CalculateFileHashAsync(string filePath)
        {
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            using var sha256 = SHA256.Create();
            var hashBytes = await Task.Run(() => sha256.ComputeHash(stream));
            return Convert.ToHexString(hashBytes);
        }

        /// <summary>
        /// Generate detailed validation report
        /// </summary>
        /// <param name="validationResult">Validation result</param>
        /// <returns>Formatted report</returns>
        public string GenerateValidationReport(ValidationResult validationResult)
        {
            var report = new StringBuilder();
            
            report.AppendLine("==================================================");
            report.AppendLine("        LEGACY MODERNIZATION OUTPUT VALIDATION");
            report.AppendLine("==================================================");
            report.AppendLine($"Job Number: {validationResult.JobNumber}");
            report.AppendLine($"Validation Time: {validationResult.ValidationStartTime:yyyy-MM-dd HH:mm:ss}");
            report.AppendLine($"Duration: {validationResult.ValidationEndTime - validationResult.ValidationStartTime:mm\\:ss\\.fff}");
            report.AppendLine($"Overall Result: {(validationResult.Success ? "✅ PASSED" : "❌ FAILED")}");
            report.AppendLine();
            
            report.AppendLine("SUMMARY:");
            report.AppendLine($"  Total Files Validated: {validationResult.TotalFilesValidated}");
            report.AppendLine($"  Files Matched: {validationResult.FilesMatched}");
            report.AppendLine($"  Files Mismatched: {validationResult.FilesMismatched}");
            report.AppendLine();

            report.AppendLine("FILE VALIDATION DETAILS:");
            report.AppendLine("--------------------------------------------------");

            foreach (var fileValidation in validationResult.FileValidations)
            {
                var status = fileValidation.IsValid ? "✅ PASS" : "❌ FAIL";
                report.AppendLine($"{status} {fileValidation.FileName}");
                
                if (fileValidation.ActualFileSize.HasValue && fileValidation.ExpectedFileSize.HasValue)
                {
                    report.AppendLine($"     Size: {fileValidation.ActualFileSize} bytes (expected: {fileValidation.ExpectedFileSize})");
                }
                
                if (!string.IsNullOrEmpty(fileValidation.ActualFileHash) && !string.IsNullOrEmpty(fileValidation.ExpectedFileHash))
                {
                    var hashMatch = fileValidation.ActualFileHash == fileValidation.ExpectedFileHash ? "✅" : "❌";
                    report.AppendLine($"     Hash: {hashMatch} {fileValidation.ActualFileHash.Substring(0, 16)}...");
                }
                
                report.AppendLine($"     Result: {fileValidation.ValidationMessage}");
                
                if (fileValidation.FirstDifferenceOffset.HasValue)
                {
                    report.AppendLine($"     First Difference: Offset {fileValidation.FirstDifferenceOffset}");
                }
                
                report.AppendLine();
            }

            if (!validationResult.Success)
            {
                report.AppendLine("RECOMMENDATIONS:");
                report.AppendLine("--------------------------------------------------");
                
                var failedFiles = validationResult.FileValidations.Where(v => !v.IsValid).ToList();
                foreach (var failed in failedFiles)
                {
                    report.AppendLine($"• {failed.FileName}: {failed.ValidationMessage}");
                }
            }

            report.AppendLine("==================================================");

            return report.ToString();
        }
    }

    /// <summary>
    /// Overall validation result
    /// </summary>
    public class ValidationResult
    {
        public string JobNumber { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public DateTime ValidationStartTime { get; set; }
        public DateTime ValidationEndTime { get; set; }
        public string ExpectedOutputPath { get; set; } = string.Empty;
        public string ActualOutputPath { get; set; } = string.Empty;
        public int TotalFilesValidated { get; set; }
        public int FilesMatched { get; set; }
        public int FilesMismatched { get; set; }
        public List<FileValidationResult> FileValidations { get; set; } = new List<FileValidationResult>();
    }

    /// <summary>
    /// Individual file validation result
    /// </summary>
    public class FileValidationResult
    {
        public string FileName { get; set; } = string.Empty;
        public bool IsValid { get; set; }
        public string ValidationMessage { get; set; } = string.Empty;
        public DateTime ValidationStartTime { get; set; }
        public DateTime ValidationEndTime { get; set; }
        public string ActualFilePath { get; set; } = string.Empty;
        public string ExpectedFilePath { get; set; } = string.Empty;
        public long? ActualFileSize { get; set; }
        public long? ExpectedFileSize { get; set; }
        public string ActualFileHash { get; set; } = string.Empty;
        public string ExpectedFileHash { get; set; } = string.Empty;
        public long? FirstDifferenceOffset { get; set; }
    }

    /// <summary>
    /// Content comparison result
    /// </summary>
    public class ContentComparisonResult
    {
        public bool IsIdentical { get; set; }
        public string ActualHash { get; set; } = string.Empty;
        public string ExpectedHash { get; set; } = string.Empty;
        public string DifferenceDescription { get; set; } = string.Empty;
        public long? FirstDifferenceOffset { get; set; }
    }
}
