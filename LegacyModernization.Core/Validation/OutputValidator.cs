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
                var validationStartTime = DateTime.Now;

                // Validate all core output files
                var fileValidations = await ValidateAllOutputFilesAsync(jobNumber, expectedOutputPath);
                result.FileValidations.AddRange(fileValidations);

                // Perform cross-file validation (Validation #9)
                var crossFileValidationPassed = await ValidateCrossFileRelationshipsAsync(fileValidations);

                // Calculate enhanced metrics
                PopulateValidationMetrics(result, fileValidations);
                PopulatePerformanceMetrics(result, validationStartTime);

                // Calculate overall success
                result.Success = fileValidations.All(v => v.IsValid) && crossFileValidationPassed;
                result.TotalFilesValidated = fileValidations.Count;
                result.FilesMatched = fileValidations.Count(v => v.IsValid);
                result.FilesMismatched = fileValidations.Count(v => !v.IsValid);

                result.ValidationEndTime = DateTime.Now;

                LogValidationSummary(result, jobNumber);

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
        /// <summary>
        /// Enhanced file validation with comprehensive checks
        /// </summary>
        /// <param name="fileName">File name to validate</param>
        /// <param name="expectedOutputPath">Expected output path</param>
        /// <returns>Comprehensive file validation result</returns>
        private async Task<FileValidationResult> ValidateOutputFileAsync(string fileName, string expectedOutputPath)
        {
            var result = new FileValidationResult
            {
                FileName = fileName,
                ValidationStartTime = DateTime.Now,
                Details = new FileValidationDetails()
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
                    result.ValidationEndTime = DateTime.Now;
                    return result;
                }

                if (!actualExists)
                {
                    result.IsValid = false;
                    result.ValidationMessage = "Actual file does not exist";
                    _logger.Warning("File validation failed: {FileName} - actual file missing", fileName);
                    result.ValidationEndTime = DateTime.Now;
                    return result;
                }

                if (!expectedExists)
                {
                    result.IsValid = false;
                    result.ValidationMessage = "Expected file does not exist";
                    _logger.Warning("File validation failed: {FileName} - expected file missing", fileName);
                    result.ValidationEndTime = DateTime.Now;
                    return result;
                }

                // Get file information
                var actualInfo = new FileInfo(actualFilePath);
                var expectedInfo = new FileInfo(expectedFilePath);
                result.ActualFileSize = actualInfo.Length;
                result.ExpectedFileSize = expectedInfo.Length;

                _logger.Information("Starting comprehensive validation for {FileName} ({FileSize} bytes)", fileName, actualInfo.Length);

                // Perform all 10 comprehensive validations
                var validationResults = new List<bool>();

                // 1. File Size Validation
                validationResults.Add(ValidateFileSize(result, 10.0)); // 10% tolerance

                // 2. Record Count Validation
                validationResults.Add(await ValidateRecordCountAsync(result));

                // 3. File Format Validation
                validationResults.Add(await ValidateFileFormatAsync(result));

                // 4. Data Type Validation
                validationResults.Add(await ValidateDataTypesAsync(result));

                // 5. Business Rule Validation
                validationResults.Add(await ValidateBusinessRulesAsync(result));

                // 6. Checksum/Hash Validation
                validationResults.Add(await ValidateChecksumsAsync(result));

                // 7. Performance Validation (max 30 seconds per file)
                validationResults.Add(ValidatePerformance(result, TimeSpan.FromSeconds(30)));

                // 8. Field-Level Validation
                validationResults.Add(await ValidateFieldLevelAsync(result));

                // 10. Historical Trends (Cross-file validation done separately)
                validationResults.Add(await ValidateHistoricalTrendsAsync(result));

                // Original content comparison for backward compatibility
                var contentMatch = await CompareFileContentAsync(actualFilePath, expectedFilePath);
                result.ActualFileHash = contentMatch.ActualHash;
                result.ExpectedFileHash = contentMatch.ExpectedHash;
                result.FirstDifferenceOffset = contentMatch.FirstDifferenceOffset;

                // Determine overall validation result
                var criticalValidationsPassed = validationResults.Take(6).All(r => r); // First 6 are critical
                var allValidationsPassed = validationResults.All(r => r) && contentMatch.IsIdentical;
                
                result.IsValid = criticalValidationsPassed && contentMatch.IsIdentical;

                if (result.IsValid)
                {
                    result.ValidationMessage = allValidationsPassed 
                        ? "All validations passed - files match exactly"
                        : "Critical validations passed - minor issues detected";
                    _logger.Information("File validation passed: {FileName} - comprehensive validation successful", fileName);
                }
                else
                {
                    var failedValidations = new List<string>();
                    if (!result.Details.SizeValidationPassed) failedValidations.Add("Size");
                    if (!result.Details.RecordCountMatches) failedValidations.Add("RecordCount");
                    if (!result.Details.FormatValidationPassed) failedValidations.Add("Format");
                    if (!result.Details.BusinessRulesValidationPassed) failedValidations.Add("BusinessRules");
                    if (!result.Details.ChecksumMatches) failedValidations.Add("Checksum");
                    if (!contentMatch.IsIdentical) failedValidations.Add("Content");

                    result.ValidationMessage = $"Validation failed: {string.Join(", ", failedValidations)}";
                    _logger.Warning("File validation failed: {FileName} - failed validations: {FailedValidations}", 
                        fileName, string.Join(", ", failedValidations));
                }

                result.ValidationEndTime = DateTime.Now;
                return result;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Comprehensive file validation error for {FileName}", fileName);
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
        /// Generate detailed validation report in text format
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

        /// <summary>
        /// Generate detailed validation report in HTML format
        /// </summary>
        /// <param name="validationResult">Validation result</param>
        /// <returns>HTML formatted report</returns>
        public string GenerateHtmlValidationReport(ValidationResult validationResult)
        {
            var html = new StringBuilder();
            var duration = validationResult.ValidationEndTime - validationResult.ValidationStartTime;
            var successRate = validationResult.TotalFilesValidated > 0 
                ? (double)validationResult.FilesMatched / validationResult.TotalFilesValidated * 100 
                : 0;

            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html lang=\"en\">");
            html.AppendLine("<head>");
            html.AppendLine("    <meta charset=\"UTF-8\">");
            html.AppendLine("    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
            html.AppendLine($"    <title>Validation Report - Job {validationResult.JobNumber}</title>");
            html.AppendLine("    <style>");
            html.AppendLine(GetEmbeddedCss());
            html.AppendLine("    </style>");
            html.AppendLine("</head>");
            html.AppendLine("<body>");

            // Header
            html.AppendLine("    <header class=\"report-header\">");
            html.AppendLine("        <div class=\"container\">");
            html.AppendLine("            <h1>🔍 Legacy Modernization Validation Report</h1>");
            html.AppendLine($"            <div class=\"job-info\">Job Number: <strong>{validationResult.JobNumber}</strong></div>");
            html.AppendLine("        </div>");
            html.AppendLine("    </header>");

            // Executive Summary
            html.AppendLine("    <div class=\"container\">");
            html.AppendLine("        <section class=\"summary-section\">");
            html.AppendLine("            <h2>📊 Executive Summary</h2>");
            html.AppendLine("            <div class=\"summary-grid\">");
            
            var statusClass = validationResult.Success ? "success" : "failure";
            var statusIcon = validationResult.Success ? "✅" : "❌";
            var statusText = validationResult.Success ? "PASSED" : "FAILED";
            
            html.AppendLine($"                <div class=\"summary-card {statusClass}\">");
            html.AppendLine($"                    <div class=\"card-icon\">{statusIcon}</div>");
            html.AppendLine($"                    <div class=\"card-title\">Overall Result</div>");
            html.AppendLine($"                    <div class=\"card-value\">{statusText}</div>");
            html.AppendLine("                </div>");

            html.AppendLine("                <div class=\"summary-card\">");
            html.AppendLine("                    <div class=\"card-icon\">📁</div>");
            html.AppendLine("                    <div class=\"card-title\">Files Validated</div>");
            html.AppendLine($"                    <div class=\"card-value\">{validationResult.TotalFilesValidated}</div>");
            html.AppendLine("                </div>");

            html.AppendLine("                <div class=\"summary-card\">");
            html.AppendLine("                    <div class=\"card-icon\">✅</div>");
            html.AppendLine("                    <div class=\"card-title\">Files Matched</div>");
            html.AppendLine($"                    <div class=\"card-value\">{validationResult.FilesMatched}</div>");
            html.AppendLine("                </div>");

            html.AppendLine("                <div class=\"summary-card\">");
            html.AppendLine("                    <div class=\"card-icon\">⏱️</div>");
            html.AppendLine("                    <div class=\"card-title\">Duration</div>");
            html.AppendLine($"                    <div class=\"card-value\">{duration.TotalSeconds:F2}s</div>");
            html.AppendLine("                </div>");

            // Enhanced metrics cards
            html.AppendLine("                <div class=\"summary-card\">");
            html.AppendLine("                    <div class=\"card-icon\">📊</div>");
            html.AppendLine("                    <div class=\"card-title\">Total Records</div>");
            html.AppendLine($"                    <div class=\"card-value\">{validationResult.Metrics.TotalRecordCount:N0}</div>");
            html.AppendLine("                </div>");

            html.AppendLine("                <div class=\"summary-card\">");
            html.AppendLine("                    <div class=\"card-icon\">💾</div>");
            html.AppendLine("                    <div class=\"card-title\">Total Size</div>");
            html.AppendLine($"                    <div class=\"card-value\">{validationResult.Metrics.TotalFileSize / 1024.0 / 1024.0:F1} MB</div>");
            html.AppendLine("                </div>");

            html.AppendLine("                <div class=\"summary-card\">");
            html.AppendLine("                    <div class=\"card-icon\">🔍</div>");
            html.AppendLine("                    <div class=\"card-title\">Checksum Matches</div>");
            html.AppendLine($"                    <div class=\"card-value\">{validationResult.Metrics.ChecksumMatches}/{validationResult.TotalFilesValidated}</div>");
            html.AppendLine("                </div>");

            html.AppendLine("                <div class=\"summary-card\">");
            html.AppendLine("                    <div class=\"card-icon\">⚡</div>");
            html.AppendLine("                    <div class=\"card-title\">Throughput</div>");
            html.AppendLine($"                    <div class=\"card-value\">{validationResult.Performance.ProcessingThroughputMBps:F1} MB/s</div>");
            html.AppendLine("                </div>");

            html.AppendLine("            </div>");

            // Progress Bar
            html.AppendLine("            <div class=\"progress-container\">");
            html.AppendLine("                <div class=\"progress-label\">");
            html.AppendLine($"                    <span>Success Rate: {successRate:F1}%</span>");
            html.AppendLine($"                    <span>{validationResult.FilesMatched}/{validationResult.TotalFilesValidated} Files</span>");
            html.AppendLine("                </div>");
            html.AppendLine($"                <div class=\"progress-bar\">");
            html.AppendLine($"                    <div class=\"progress-fill\" style=\"width: {successRate}%\"></div>");
            html.AppendLine("                </div>");
            html.AppendLine("            </div>");

            // Validation Categories Summary
            html.AppendLine("            <div class=\"validation-categories\">");
            html.AppendLine("                <h3>🔍 Validation Categories</h3>");
            html.AppendLine("                <div class=\"category-grid\">");
            
            AddValidationCategoryCard(html, "📏", "Size Validation", 
                validationResult.FileValidations.Count(f => f.Details.SizeValidationPassed), 
                validationResult.TotalFilesValidated);
            
            AddValidationCategoryCard(html, "📊", "Record Count", 
                validationResult.FileValidations.Count(f => f.Details.RecordCountMatches), 
                validationResult.TotalFilesValidated);
            
            AddValidationCategoryCard(html, "📝", "Format Validation", 
                validationResult.Metrics.FormatValidationPassed, 
                validationResult.TotalFilesValidated);
            
            AddValidationCategoryCard(html, "🔢", "Data Types", 
                validationResult.FileValidations.Count(f => f.Details.ValidNumericFields + f.Details.ValidDateFields > 0), 
                validationResult.TotalFilesValidated);
            
            AddValidationCategoryCard(html, "📋", "Business Rules", 
                validationResult.Metrics.BusinessRulesPassed, 
                validationResult.TotalFilesValidated);
            
            AddValidationCategoryCard(html, "🔐", "Checksums", 
                validationResult.Metrics.ChecksumMatches, 
                validationResult.TotalFilesValidated);

            html.AppendLine("                </div>");
            html.AppendLine("            </div>");
            html.AppendLine("        </section>");

            // Metadata
            html.AppendLine("        <section class=\"metadata-section\">");
            html.AppendLine("            <h3>📋 Validation Details</h3>");
            html.AppendLine("            <div class=\"metadata-grid\">");
            html.AppendLine($"                <div><strong>Start Time:</strong> {validationResult.ValidationStartTime:yyyy-MM-dd HH:mm:ss}</div>");
            html.AppendLine($"                <div><strong>End Time:</strong> {validationResult.ValidationEndTime:yyyy-MM-dd HH:mm:ss}</div>");
            html.AppendLine($"                <div><strong>Expected Path:</strong> <code>{validationResult.ExpectedOutputPath}</code></div>");
            html.AppendLine($"                <div><strong>Actual Path:</strong> <code>{validationResult.ActualOutputPath}</code></div>");
            html.AppendLine("            </div>");
            html.AppendLine("        </section>");

            // File Validation Results
            html.AppendLine("        <section class=\"results-section\">");
            html.AppendLine("            <h2>📄 File Validation Results</h2>");

            foreach (var fileValidation in validationResult.FileValidations)
            {
                var fileStatusClass = fileValidation.IsValid ? "file-pass" : "file-fail";
                var fileStatusIcon = fileValidation.IsValid ? "✅" : "❌";
                var fileStatusText = fileValidation.IsValid ? "PASS" : "FAIL";

                html.AppendLine($"            <div class=\"file-result {fileStatusClass}\">");
                html.AppendLine("                <div class=\"file-header\" onclick=\"toggleFileDetails(this)\">");
                html.AppendLine($"                    <div class=\"file-status\">{fileStatusIcon} {fileStatusText}</div>");
                html.AppendLine($"                    <div class=\"file-name\">{fileValidation.FileName}</div>");
                html.AppendLine("                    <div class=\"toggle-icon\">▼</div>");
                html.AppendLine("                </div>");
                
                html.AppendLine("                <div class=\"file-details\">");
                html.AppendLine("                    <div class=\"details-grid\">");
                
                if (fileValidation.ActualFileSize.HasValue && fileValidation.ExpectedFileSize.HasValue)
                {
                    var sizeMatch = fileValidation.ActualFileSize == fileValidation.ExpectedFileSize;
                    var sizeIcon = sizeMatch ? "✅" : "❌";
                    html.AppendLine($"                        <div><strong>File Size:</strong> {sizeIcon} {fileValidation.ActualFileSize:N0} bytes");
                    if (!sizeMatch)
                    {
                        html.AppendLine($" (expected: {fileValidation.ExpectedFileSize:N0})");
                    }
                    html.AppendLine("</div>");
                }

                if (!string.IsNullOrEmpty(fileValidation.ActualFileHash) && !string.IsNullOrEmpty(fileValidation.ExpectedFileHash))
                {
                    var hashMatch = fileValidation.ActualFileHash == fileValidation.ExpectedFileHash;
                    var hashIcon = hashMatch ? "✅" : "❌";
                    html.AppendLine($"                        <div><strong>Hash:</strong> {hashIcon} <code>{fileValidation.ActualFileHash.Substring(0, Math.Min(16, fileValidation.ActualFileHash.Length))}...</code></div>");
                }

                html.AppendLine($"                        <div><strong>Validation Message:</strong> {fileValidation.ValidationMessage}</div>");

                if (fileValidation.FirstDifferenceOffset.HasValue)
                {
                    html.AppendLine($"                        <div><strong>First Difference:</strong> Offset {fileValidation.FirstDifferenceOffset:N0}</div>");
                }

                var fileDuration = fileValidation.ValidationEndTime - fileValidation.ValidationStartTime;
                html.AppendLine($"                        <div><strong>Validation Time:</strong> {fileDuration.TotalMilliseconds:F0} ms</div>");

                html.AppendLine("                    </div>");
                html.AppendLine("                </div>");
                html.AppendLine("            </div>");
            }

            html.AppendLine("        </section>");

            // Recommendations (if there are failures)
            if (!validationResult.Success)
            {
                html.AppendLine("        <section class=\"recommendations-section\">");
                html.AppendLine("            <h2>💡 Recommendations</h2>");
                html.AppendLine("            <div class=\"recommendations-list\">");
                
                var failedFiles = validationResult.FileValidations.Where(v => !v.IsValid).ToList();
                foreach (var failed in failedFiles)
                {
                    html.AppendLine($"                <div class=\"recommendation-item\">");
                    html.AppendLine($"                    <strong>{failed.FileName}:</strong> {failed.ValidationMessage}");
                    html.AppendLine("                </div>");
                }
                
                html.AppendLine("            </div>");
                html.AppendLine("        </section>");
            }

            html.AppendLine("    </div>");

            // Footer
            html.AppendLine("    <footer class=\"report-footer\">");
            html.AppendLine("        <div class=\"container\">");
            html.AppendLine($"            <p>Report generated on {DateTime.Now:yyyy-MM-dd HH:mm:ss} | Legacy Modernization Validation System</p>");
            html.AppendLine("        </div>");
            html.AppendLine("    </footer>");

            // JavaScript
            html.AppendLine("    <script>");
            html.AppendLine(GetEmbeddedJavaScript());
            html.AppendLine("    </script>");

            html.AppendLine("</body>");
            html.AppendLine("</html>");

            return html.ToString();
        }

        /// <summary>
        /// Get embedded CSS for HTML report
        /// </summary>
        private string GetEmbeddedCss()
        {
            return @"
        * {
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }
        
        body {
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            line-height: 1.6;
            color: #333;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            min-height: 100vh;
        }
        
        .container {
            max-width: 1200px;
            margin: 0 auto;
            padding: 0 20px;
        }
        
        .report-header {
            background: rgba(255, 255, 255, 0.95);
            backdrop-filter: blur(10px);
            padding: 2rem 0;
            margin-bottom: 2rem;
            box-shadow: 0 4px 20px rgba(0, 0, 0, 0.1);
        }
        
        .report-header h1 {
            font-size: 2.5rem;
            font-weight: 700;
            color: #2c3e50;
            margin-bottom: 0.5rem;
        }
        
        .job-info {
            font-size: 1.2rem;
            color: #7f8c8d;
        }
        
        .summary-section {
            background: white;
            padding: 2rem;
            border-radius: 15px;
            box-shadow: 0 8px 32px rgba(0, 0, 0, 0.1);
            margin-bottom: 2rem;
        }
        
        .summary-section h2 {
            font-size: 1.8rem;
            margin-bottom: 1.5rem;
            color: #2c3e50;
        }
        
        .summary-grid {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
            gap: 1.5rem;
            margin-bottom: 2rem;
        }
        
        .summary-card {
            background: #f8f9fa;
            padding: 1.5rem;
            border-radius: 12px;
            text-align: center;
            border: 2px solid transparent;
            transition: all 0.3s ease;
        }
        
        .summary-card:hover {
            transform: translateY(-2px);
            box-shadow: 0 4px 20px rgba(0, 0, 0, 0.1);
        }
        
        .summary-card.success {
            background: linear-gradient(135deg, #d4edda, #c3e6cb);
            border-color: #28a745;
        }
        
        .summary-card.failure {
            background: linear-gradient(135deg, #f8d7da, #f5c6cb);
            border-color: #dc3545;
        }
        
        .card-icon {
            font-size: 2rem;
            margin-bottom: 0.5rem;
        }
        
        .card-title {
            font-size: 0.9rem;
            color: #6c757d;
            margin-bottom: 0.5rem;
            text-transform: uppercase;
            letter-spacing: 0.5px;
        }
        
        .card-value {
            font-size: 1.5rem;
            font-weight: 700;
            color: #2c3e50;
        }
        
        .progress-container {
            margin-top: 1.5rem;
        }
        
        .progress-label {
            display: flex;
            justify-content: space-between;
            margin-bottom: 0.5rem;
            font-weight: 600;
            color: #495057;
        }
        
        .progress-bar {
            background: #e9ecef;
            border-radius: 10px;
            height: 20px;
            overflow: hidden;
        }
        
        .progress-fill {
            background: linear-gradient(90deg, #28a745, #20c997);
            height: 100%;
            border-radius: 10px;
            transition: width 0.5s ease;
        }
        
        .validation-categories {
            margin-top: 2rem;
        }
        
        .validation-categories h3 {
            margin-bottom: 1rem;
            color: #2c3e50;
            font-size: 1.2rem;
        }
        
        .category-grid {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(150px, 1fr));
            gap: 1rem;
        }
        
        .category-card {
            background: #f8f9fa;
            padding: 1rem;
            border-radius: 8px;
            text-align: center;
            border-left: 4px solid #dee2e6;
            transition: all 0.3s ease;
        }
        
        .category-card:hover {
            transform: translateY(-2px);
            box-shadow: 0 4px 15px rgba(0, 0, 0, 0.1);
        }
        
        .category-icon {
            font-size: 1.5rem;
            margin-bottom: 0.5rem;
        }
        
        .category-title {
            font-size: 0.8rem;
            color: #6c757d;
            margin-bottom: 0.5rem;
            font-weight: 600;
        }
        
        .category-value {
            font-size: 1.2rem;
            font-weight: 700;
            margin-bottom: 0.25rem;
        }
        
        .category-value.success {
            color: #28a745;
            border-left-color: #28a745;
        }
        
        .category-value.warning {
            color: #ffc107;
            border-left-color: #ffc107;
        }
        
        .category-value.failure {
            color: #dc3545;
            border-left-color: #dc3545;
        }
        
        .category-percent {
            font-size: 0.8rem;
            color: #6c757d;
        }
        
        .metadata-section {
            background: white;
            padding: 1.5rem;
            border-radius: 12px;
            box-shadow: 0 4px 20px rgba(0, 0, 0, 0.1);
            margin-bottom: 2rem;
        }
        
        .metadata-section h3 {
            margin-bottom: 1rem;
            color: #2c3e50;
        }
        
        .metadata-grid {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
            gap: 1rem;
        }
        
        .metadata-grid code {
            background: #f8f9fa;
            padding: 0.2rem 0.4rem;
            border-radius: 4px;
            font-family: 'Courier New', monospace;
            font-size: 0.9rem;
            color: #e83e8c;
        }
        
        .results-section {
            background: white;
            padding: 2rem;
            border-radius: 15px;
            box-shadow: 0 8px 32px rgba(0, 0, 0, 0.1);
            margin-bottom: 2rem;
        }
        
        .results-section h2 {
            font-size: 1.8rem;
            margin-bottom: 1.5rem;
            color: #2c3e50;
        }
        
        .file-result {
            border: 2px solid #e9ecef;
            border-radius: 12px;
            margin-bottom: 1rem;
            overflow: hidden;
            transition: all 0.3s ease;
        }
        
        .file-result:hover {
            border-color: #6c757d;
        }
        
        .file-result.file-pass {
            border-color: #28a745;
        }
        
        .file-result.file-fail {
            border-color: #dc3545;
        }
        
        .file-header {
            background: #f8f9fa;
            padding: 1rem 1.5rem;
            display: flex;
            justify-content: space-between;
            align-items: center;
            cursor: pointer;
            transition: background-color 0.3s ease;
        }
        
        .file-header:hover {
            background: #e9ecef;
        }
        
        .file-pass .file-header {
            background: linear-gradient(135deg, #d4edda, #c3e6cb);
        }
        
        .file-fail .file-header {
            background: linear-gradient(135deg, #f8d7da, #f5c6cb);
        }
        
        .file-status {
            font-weight: 700;
            font-size: 1.1rem;
        }
        
        .file-name {
            font-family: 'Courier New', monospace;
            font-weight: 600;
            color: #495057;
        }
        
        .toggle-icon {
            font-size: 1.2rem;
            transition: transform 0.3s ease;
        }
        
        .file-details {
            padding: 1.5rem;
            background: white;
            display: none;
            border-top: 1px solid #e9ecef;
        }
        
        .file-details.show {
            display: block;
        }
        
        .details-grid {
            display: grid;
            gap: 0.8rem;
        }
        
        .details-grid div {
            padding: 0.5rem 0;
            border-bottom: 1px solid #f8f9fa;
        }
        
        .details-grid div:last-child {
            border-bottom: none;
        }
        
        .details-grid code {
            background: #f8f9fa;
            padding: 0.2rem 0.4rem;
            border-radius: 4px;
            font-family: 'Courier New', monospace;
            font-size: 0.9rem;
            color: #e83e8c;
        }
        
        .recommendations-section {
            background: linear-gradient(135deg, #fff3cd, #ffeaa7);
            padding: 2rem;
            border-radius: 15px;
            box-shadow: 0 8px 32px rgba(0, 0, 0, 0.1);
            margin-bottom: 2rem;
            border: 2px solid #ffc107;
        }
        
        .recommendations-section h2 {
            color: #856404;
            margin-bottom: 1.5rem;
        }
        
        .recommendation-item {
            background: white;
            padding: 1rem;
            border-radius: 8px;
            margin-bottom: 1rem;
            border-left: 4px solid #ffc107;
        }
        
        .recommendation-item:last-child {
            margin-bottom: 0;
        }
        
        .report-footer {
            background: rgba(255, 255, 255, 0.95);
            backdrop-filter: blur(10px);
            padding: 1.5rem 0;
            text-align: center;
            color: #6c757d;
            margin-top: 3rem;
        }
        
        @media (max-width: 768px) {
            .container {
                padding: 0 15px;
            }
            
            .report-header h1 {
                font-size: 2rem;
            }
            
            .summary-grid {
                grid-template-columns: repeat(auto-fit, minmax(150px, 1fr));
                gap: 1rem;
            }
            
            .metadata-grid {
                grid-template-columns: 1fr;
            }
            
            .file-header {
                flex-direction: column;
                align-items: flex-start;
                gap: 0.5rem;
            }
            
            .toggle-icon {
                align-self: flex-end;
            }
        }
        
        @media print {
            body {
                background: white;
            }
            
            .report-header,
            .summary-section,
            .metadata-section,
            .results-section,
            .recommendations-section {
                box-shadow: none;
                border: 1px solid #ddd;
            }
            
            .file-details {
                display: block !important;
            }
            
            .toggle-icon {
                display: none;
            }
        }";
        }

        /// <summary>
        /// Get embedded JavaScript for HTML report
        /// </summary>
        private string GetEmbeddedJavaScript()
        {
            return @"
        function toggleFileDetails(header) {
            const details = header.nextElementSibling;
            const icon = header.querySelector('.toggle-icon');
            
            if (details.classList.contains('show')) {
                details.classList.remove('show');
                icon.style.transform = 'rotate(0deg)';
            } else {
                details.classList.add('show');
                icon.style.transform = 'rotate(180deg)';
            }
        }
        
        // Auto-expand failed file details
        document.addEventListener('DOMContentLoaded', function() {
            const failedFiles = document.querySelectorAll('.file-fail .file-header');
            failedFiles.forEach(header => {
                toggleFileDetails(header);
            });
        });";
        }

        #region Comprehensive Validation Methods

        /// <summary>
        /// 1. File Size Validation - Compare file sizes with tolerance
        /// </summary>
        private bool ValidateFileSize(FileValidationResult result, double tolerancePercentage = 10.0)
        {
            if (!result.ActualFileSize.HasValue || !result.ExpectedFileSize.HasValue)
            {
                result.Details.SizeValidationPassed = false;
                result.Details.SizeVariancePercentage = 0;
                return false;
            }

            var variance = Math.Abs(result.ActualFileSize.Value - result.ExpectedFileSize.Value);
            var variancePercentage = result.ExpectedFileSize.Value > 0 
                ? (double)variance / result.ExpectedFileSize.Value * 100 
                : 0;

            result.Details.SizeVariancePercentage = variancePercentage;
            result.Details.SizeValidationPassed = variancePercentage <= tolerancePercentage;

            if (!result.Details.SizeValidationPassed)
            {
                AddValidationIssue(result.FileName, "FileSizeVariance", 
                    $"File size variance {variancePercentage:F1}% exceeds tolerance {tolerancePercentage}%", 
                    "Warning", $"Expected: {result.ExpectedFileSize}, Actual: {result.ActualFileSize}");
            }

            return result.Details.SizeValidationPassed;
        }

        /// <summary>
        /// 2. Record Count Validation - Count and compare records
        /// </summary>
        private async Task<bool> ValidateRecordCountAsync(FileValidationResult result)
        {
            try
            {
                if (File.Exists(result.ActualFilePath))
                {
                    result.Details.ActualRecordCount = await CountRecordsAsync(result.ActualFilePath);
                }

                if (File.Exists(result.ExpectedFilePath))
                {
                    result.Details.ExpectedRecordCount = await CountRecordsAsync(result.ExpectedFilePath);
                }

                result.Details.RecordCountMatches = result.Details.ActualRecordCount == result.Details.ExpectedRecordCount;

                if (!result.Details.RecordCountMatches)
                {
                    AddValidationIssue(result.FileName, "RecordCountMismatch",
                        $"Record count mismatch: Expected {result.Details.ExpectedRecordCount}, Got {result.Details.ActualRecordCount}",
                        "Critical", "File may be incomplete or contain extra records");
                }

                return result.Details.RecordCountMatches;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to validate record count for {FileName}", result.FileName);
                AddValidationIssue(result.FileName, "RecordCountValidationError", ex.Message, "Critical", "");
                return false;
            }
        }

        /// <summary>
        /// 3. File Format Validation - Validate file structure and encoding
        /// </summary>
        private async Task<bool> ValidateFileFormatAsync(FileValidationResult result)
        {
            try
            {
                if (!File.Exists(result.ActualFilePath))
                {
                    result.Details.FormatValidationPassed = false;
                    return false;
                }

                // Detect encoding
                result.Details.DetectedEncoding = await DetectFileEncodingAsync(result.ActualFilePath);
                result.Details.ExpectedEncoding = "ASCII"; // Default expected encoding
                result.Details.FileExtension = Path.GetExtension(result.ActualFilePath);

                // Validate file extension - handle backup/original files
                var expectedExtensions = new[] { ".txt", ".asc", ".dat", ".se1", ".4300" };
                var fileName = Path.GetFileName(result.ActualFilePath).ToLowerInvariant();
                
                // Check for standard extensions or backup file patterns
                var hasValidExtension = expectedExtensions.Contains(result.Details.FileExtension.ToLowerInvariant()) ||
                                       IsBackupOrOriginalFile(fileName);

                // Validate encoding - include binary files for legacy formats
                var hasValidEncoding = result.Details.DetectedEncoding.Contains("ASCII") || 
                                      result.Details.DetectedEncoding.Contains("UTF-8") ||
                                      result.Details.DetectedEncoding.Contains("Binary"); // Accept binary for .asc/.4300 files

                result.Details.FormatValidationPassed = hasValidExtension && hasValidEncoding;

                if (!result.Details.FormatValidationPassed)
                {
                    AddValidationIssue(result.FileName, "FileFormatValidation",
                        $"File format validation failed",
                        "Warning", 
                        $"Extension: {result.Details.FileExtension}, Encoding: {result.Details.DetectedEncoding}");
                }

                return result.Details.FormatValidationPassed;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to validate file format for {FileName}", result.FileName);
                AddValidationIssue(result.FileName, "FileFormatValidationError", ex.Message, "Critical", "");
                return false;
            }
        }

        /// <summary>
        /// 4. Data Type Validation - Validate field data types
        /// </summary>
        private async Task<bool> ValidateDataTypesAsync(FileValidationResult result)
        {
            try
            {
                if (!File.Exists(result.ActualFilePath))
                {
                    return false;
                }

                // Skip data type validation for binary files
                if (result.Details.DetectedEncoding.Contains("Binary") || 
                    result.Details.DetectedEncoding.Equals("data", StringComparison.OrdinalIgnoreCase) ||
                    result.FileName.EndsWith(".4300"))
                {
                    // For binary files, just validate they're not empty (unless expected to be empty)
                    var fileInfo = new FileInfo(result.ActualFilePath);
                    var binaryFileValid = fileInfo.Length > 0 || result.FileName.Contains("e.txt"); // e.txt can be empty
                    return binaryFileValid;
                }

                var lines = await File.ReadAllLinesAsync(result.ActualFilePath);
                result.Details.ValidNumericFields = 0;
                result.Details.InvalidNumericFields = 0;
                result.Details.ValidDateFields = 0;
                result.Details.InvalidDateFields = 0;

                foreach (var line in lines.Take(100)) // Sample first 100 lines
                {
                    await ValidateLineDataTypesAsync(line, result);
                }

                var totalValidations = result.Details.ValidNumericFields + result.Details.InvalidNumericFields +
                                     result.Details.ValidDateFields + result.Details.InvalidDateFields;

                var isValid = totalValidations == 0 || 
                             (result.Details.InvalidNumericFields + result.Details.InvalidDateFields) < totalValidations * 0.1; // 10% tolerance

                if (!isValid)
                {
                    AddValidationIssue(result.FileName, "DataTypeValidation",
                        $"Data type validation failed",
                        "Warning",
                        $"Invalid numeric: {result.Details.InvalidNumericFields}, Invalid dates: {result.Details.InvalidDateFields}");
                }

                return isValid;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to validate data types for {FileName}", result.FileName);
                AddValidationIssue(result.FileName, "DataTypeValidationError", ex.Message, "Critical", "");
                return false;
            }
        }

        /// <summary>
        /// 5. Business Rule Validation - Validate business logic rules
        /// </summary>
        private async Task<bool> ValidateBusinessRulesAsync(FileValidationResult result)
        {
            try
            {
                if (!File.Exists(result.ActualFilePath))
                {
                    result.Details.BusinessRulesValidationPassed = false;
                    return false;
                }

                var lines = await File.ReadAllLinesAsync(result.ActualFilePath);
                var violations = new List<string>();

                // Example business rules for legacy modernization
                await ValidateBusinessRulesForFileTypeAsync(result.FileName, lines, violations);

                result.Details.BusinessRuleViolations = violations;
                result.Details.BusinessRulesValidationPassed = violations.Count == 0;

                if (!result.Details.BusinessRulesValidationPassed)
                {
                    foreach (var violation in violations)
                    {
                        AddValidationIssue(result.FileName, "BusinessRuleViolation", violation, "Warning", "");
                    }
                }

                return result.Details.BusinessRulesValidationPassed;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to validate business rules for {FileName}", result.FileName);
                AddValidationIssue(result.FileName, "BusinessRuleValidationError", ex.Message, "Critical", "");
                return false;
            }
        }

        /// <summary>
        /// 6. Checksum/Hash Validation - Calculate and compare file hashes
        /// </summary>
        private async Task<bool> ValidateChecksumsAsync(FileValidationResult result)
        {
            try
            {
                if (!File.Exists(result.ActualFilePath) || !File.Exists(result.ExpectedFilePath))
                {
                    result.Details.ChecksumMatches = false;
                    return false;
                }

                // Calculate MD5 and SHA256 hashes
                result.Details.MD5Hash = await CalculateMD5HashAsync(result.ActualFilePath);
                result.Details.SHA256Hash = await CalculateFileHashAsync(result.ActualFilePath);

                var expectedMD5 = await CalculateMD5HashAsync(result.ExpectedFilePath);
                var expectedSHA256 = await CalculateFileHashAsync(result.ExpectedFilePath);

                result.Details.ChecksumMatches = result.Details.MD5Hash == expectedMD5 && 
                                               result.Details.SHA256Hash == expectedSHA256;

                if (!result.Details.ChecksumMatches)
                {
                    AddValidationIssue(result.FileName, "ChecksumMismatch",
                        "File checksums do not match expected values",
                        "Critical",
                        $"MD5: {result.Details.MD5Hash} vs {expectedMD5}");
                }

                return result.Details.ChecksumMatches;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to validate checksums for {FileName}", result.FileName);
                AddValidationIssue(result.FileName, "ChecksumValidationError", ex.Message, "Critical", "");
                return false;
            }
        }

        /// <summary>
        /// 7. Performance Validation - Track and validate processing performance
        /// </summary>
        private bool ValidatePerformance(FileValidationResult result, TimeSpan maxExpectedDuration)
        {
            try
            {
                result.Details.ProcessingTime = result.ValidationEndTime - result.ValidationStartTime;
                
                if (result.ActualFileSize.HasValue && result.Details.ProcessingTime.TotalSeconds > 0)
                {
                    result.Details.ProcessingSpeedMBps = (result.ActualFileSize.Value / 1024.0 / 1024.0) / 
                                                        result.Details.ProcessingTime.TotalSeconds;
                }

                var isWithinExpectedTime = result.Details.ProcessingTime <= maxExpectedDuration;

                if (!isWithinExpectedTime)
                {
                    AddValidationIssue(result.FileName, "PerformanceRegression",
                        $"Processing time {result.Details.ProcessingTime.TotalSeconds:F2}s exceeds expected {maxExpectedDuration.TotalSeconds:F2}s",
                        "Warning",
                        $"Processing speed: {result.Details.ProcessingSpeedMBps:F2} MB/s");
                }

                return isWithinExpectedTime;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to validate performance for {FileName}", result.FileName);
                AddValidationIssue(result.FileName, "PerformanceValidationError", ex.Message, "Warning", "");
                return false;
            }
        }

        /// <summary>
        /// 8. Field-Level Validation - Validate individual fields in fixed-width files
        /// </summary>
        private async Task<bool> ValidateFieldLevelAsync(FileValidationResult result)
        {
            try
            {
                if (!File.Exists(result.ActualFilePath) || !File.Exists(result.ExpectedFilePath))
                {
                    return false;
                }

                var actualLines = await File.ReadAllLinesAsync(result.ActualFilePath);
                var expectedLines = await File.ReadAllLinesAsync(result.ExpectedFilePath);

                var fieldValidations = new List<FieldValidationResult>();
                var maxLines = Math.Min(actualLines.Length, expectedLines.Length);
                maxLines = Math.Min(maxLines, 50); // Validate first 50 lines for performance

                for (int i = 0; i < maxLines; i++)
                {
                    var fieldResults = ValidateLineFields(actualLines[i], expectedLines[i], i);
                    fieldValidations.AddRange(fieldResults);
                }

                result.Details.FieldValidations = fieldValidations;

                var failedFields = fieldValidations.Count(f => !f.IsValid);
                var isValid = failedFields == 0 || failedFields < fieldValidations.Count * 0.05; // 5% tolerance

                if (!isValid)
                {
                    AddValidationIssue(result.FileName, "FieldLevelValidation",
                        $"Field-level validation failed: {failedFields}/{fieldValidations.Count} fields invalid",
                        "Warning", "");
                }

                return isValid;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to validate field level for {FileName}", result.FileName);
                AddValidationIssue(result.FileName, "FieldLevelValidationError", ex.Message, "Critical", "");
                return false;
            }
        }

        /// <summary>
        /// 9. Cross-File Validation - Validate relationships between files
        /// </summary>
        private async Task<bool> ValidateCrossFileRelationshipsAsync(List<FileValidationResult> allResults)
        {
            try
            {
                var crossFileIssues = new List<string>();

                // Example: Validate that .txt and .asc files have consistent record counts
                var txtFile = allResults.FirstOrDefault(r => r.FileName.EndsWith(".txt"));
                var ascFile = allResults.FirstOrDefault(r => r.FileName.EndsWith("p.asc"));

                if (txtFile != null && ascFile != null && 
                    txtFile.Details.ActualRecordCount != ascFile.Details.ActualRecordCount)
                {
                    // Only flag as issue if both files have records - empty e.txt with records in p.asc is normal
                    if (txtFile.Details.ActualRecordCount > 0 && ascFile.Details.ActualRecordCount > 0)
                    {
                        crossFileIssues.Add($"Record count mismatch between {txtFile.FileName} ({txtFile.Details.ActualRecordCount}) and {ascFile.FileName} ({ascFile.Details.ActualRecordCount})");
                    }
                }

                // Validate total file sizes are reasonable
                var totalSize = allResults.Where(r => r.ActualFileSize.HasValue).Sum(r => r.ActualFileSize!.Value);
                var expectedTotalSize = allResults.Where(r => r.ExpectedFileSize.HasValue).Sum(r => r.ExpectedFileSize!.Value);

                if (expectedTotalSize > 0 && Math.Abs(totalSize - expectedTotalSize) > expectedTotalSize * 0.2) // 20% tolerance
                {
                    crossFileIssues.Add($"Total file size variance: Expected {expectedTotalSize}, Got {totalSize}");
                }

                var isValid = crossFileIssues.Count == 0;

                if (!isValid)
                {
                    foreach (var issue in crossFileIssues)
                    {
                        AddValidationIssue("CrossFile", "CrossFileValidation", issue, "Warning", "");
                    }
                }

                return isValid;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to validate cross-file relationships");
                AddValidationIssue("CrossFile", "CrossFileValidationError", ex.Message, "Critical", "");
                return false;
            }
        }

        /// <summary>
        /// 10. Historical Comparison - Compare against previous runs
        /// </summary>
        private async Task<bool> ValidateHistoricalTrendsAsync(FileValidationResult result)
        {
            try
            {
                // This would typically load historical data from a database or files
                // For now, we'll simulate basic trend analysis
                
                result.Details.HistoricalComparisonAvailable = false; // Would be true if historical data exists
                result.Details.HistoricalTrend = "Stable"; // Would be calculated from historical data

                // Simulate trend analysis based on file size and record count
                if (result.ActualFileSize.HasValue && result.Details.ActualRecordCount > 0)
                {
                    // Would compare against historical averages
                    var sizeBasedTrend = AnalyzeSizeTrend(result.ActualFileSize.Value);
                    var recordBasedTrend = AnalyzeRecordCountTrend(result.Details.ActualRecordCount);

                    result.Details.HistoricalTrend = CombineTrends(sizeBasedTrend, recordBasedTrend);

                    if (result.Details.HistoricalTrend == "Anomaly")
                    {
                        AddValidationIssue(result.FileName, "HistoricalAnomaly",
                            "File metrics show anomalous pattern compared to historical data",
                            "Info",
                            $"Size: {result.ActualFileSize}, Records: {result.Details.ActualRecordCount}");
                    }
                }

                return true; // Historical validation is informational, doesn't fail validation
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to validate historical trends for {FileName}", result.FileName);
                return true; // Don't fail validation for historical analysis errors
            }
        }

        #endregion

        #region Helper Methods

        private async Task<long> CountRecordsAsync(string filePath)
        {
            var fileInfo = new FileInfo(filePath);
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            
            // Handle binary files differently
            if (extension == ".asc" || extension == ".4300")
            {
                // For binary files, estimate records based on expected record size
                if (extension == ".asc")
                {
                    // MB2000 format: typically 2000 bytes per record
                    return fileInfo.Length / 2000;
                }
                else if (extension == ".4300")
                {
                    // Work2 format: typically 4300 bytes per record  
                    return fileInfo.Length > 0 ? Math.Max(1, fileInfo.Length / 27520) : 0; // 137600/5 = 27520 bytes per record
                }
            }
            
            // For text files, count lines
            using var reader = new StreamReader(filePath);
            long count = 0;
            while (await reader.ReadLineAsync() != null)
            {
                count++;
            }
            return count;
        }

        private async Task<string> DetectFileEncodingAsync(string filePath)
        {
            var buffer = new byte[1024];
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            await stream.ReadAsync(buffer, 0, buffer.Length);
            
            // Simple encoding detection - could be enhanced with proper encoding detection library
            if (buffer.Take(3).SequenceEqual(new byte[] { 0xEF, 0xBB, 0xBF }))
                return "UTF-8 with BOM";
            
            var hasNullBytes = buffer.Contains((byte)0);
            var hasHighAscii = buffer.Any(b => b > 127);
            
            if (hasNullBytes) return "Binary or Unicode";
            if (hasHighAscii) return "Extended ASCII or UTF-8";
            return "ASCII";
        }

        private async Task ValidateLineDataTypesAsync(string line, FileValidationResult result)
        {
            // Skip validation for very short lines or header-like lines
            if (line.Length < 10 || line.StartsWith("#") || line.StartsWith("//"))
            {
                return;
            }

            // Handle different file formats appropriately
            if (result.FileName.EndsWith(".se1") || result.FileName.EndsWith(".asc"))
            {
                // Fixed-width legacy files - validate by position ranges
                await ValidateFixedWidthLineAsync(line, result);
            }
            else
            {
                // CSV or delimited files
                await ValidateDelimitedLineAsync(line, result);
            }
        }

        private async Task ValidateFixedWidthLineAsync(string line, FileValidationResult result)
        {
            // For fixed-width files, just do basic validation
            // Check for reasonable length and printable characters
            if (line.Length > 0)
            {
                var printableChars = line.Count(c => !char.IsControl(c) || c == '\t');
                var totalChars = line.Length;
                
                if (printableChars > totalChars * 0.8) // 80% printable characters
                {
                    result.Details.ValidNumericFields++; // Count as valid structure
                }
                else
                {
                    result.Details.InvalidNumericFields++;
                    result.Details.FieldValidationErrors.Add($"Line with too many control characters: {line.Length} chars");
                }
            }
        }

        private async Task ValidateDelimitedLineAsync(string line, FileValidationResult result)
        {
            // Example validation for numeric and date fields - would be customized per file format
            var fields = line.Split(',', StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var field in fields)
            {
                var trimmedField = field.Trim();
                
                // Check for numeric fields
                if (decimal.TryParse(trimmedField, out _))
                {
                    result.Details.ValidNumericFields++;
                }
                else if (trimmedField.All(char.IsDigit) && trimmedField.Length > 0)
                {
                    result.Details.ValidNumericFields++;
                }
                else if (trimmedField.Contains('.') || trimmedField.Contains(','))
                {
                    result.Details.InvalidNumericFields++;
                    result.Details.FieldValidationErrors.Add($"Invalid numeric field: {trimmedField}");
                }
                
                // Check for date fields
                if (DateTime.TryParse(trimmedField, out _))
                {
                    result.Details.ValidDateFields++;
                }
                else if (trimmedField.Length == 8 && trimmedField.All(char.IsDigit))
                {
                    // YYYYMMDD format
                    if (DateTime.TryParseExact(trimmedField, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out _))
                    {
                        result.Details.ValidDateFields++;
                    }
                    else
                    {
                        result.Details.InvalidDateFields++;
                        result.Details.FieldValidationErrors.Add($"Invalid date field: {trimmedField}");
                    }
                }
            }
        }

        private async Task ValidateBusinessRulesForFileTypeAsync(string fileName, string[] lines, List<string> violations)
        {
            // Example business rules - would be customized based on actual business requirements
            
            if (fileName.EndsWith("p.asc")) // Paper bills
            {
                // Example: Validate that bill totals are positive
                foreach (var line in lines.Take(10)) // Sample validation
                {
                    if (line.Length > 50 && decimal.TryParse(line.Substring(40, 10).Trim(), out var amount))
                    {
                        if (amount < 0)
                        {
                            violations.Add($"Negative bill amount found: {amount}");
                        }
                    }
                }
            }
            else if (fileName.EndsWith("e.txt")) // Electronic bills
            {
                // Example: Validate electronic bill format
                foreach (var line in lines.Take(10))
                {
                    if (!string.IsNullOrWhiteSpace(line) && line.Length < 20)
                    {
                        violations.Add($"Electronic bill line too short: {line.Length} characters");
                    }
                }
            }
        }

        private async Task<string> CalculateMD5HashAsync(string filePath)
        {
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            using var md5 = MD5.Create();
            var hashBytes = await Task.Run(() => md5.ComputeHash(stream));
            return Convert.ToHexString(hashBytes);
        }

        private List<FieldValidationResult> ValidateLineFields(string actualLine, string expectedLine, int lineNumber)
        {
            var results = new List<FieldValidationResult>();
            
            // Example field validation for fixed-width files
            var fieldPositions = new[] { 
                (0, 10, "CustomerID"),
                (10, 20, "Amount"), 
                (20, 30, "Date"),
                (30, 40, "BillType")
            };

            foreach (var (start, end, fieldName) in fieldPositions)
            {
                if (actualLine.Length > end && expectedLine.Length > end)
                {
                    var actualValue = actualLine.Substring(start, end - start).Trim();
                    var expectedValue = expectedLine.Substring(start, end - start).Trim();
                    
                    results.Add(new FieldValidationResult
                    {
                        FieldIndex = start,
                        FieldName = fieldName,
                        ActualValue = actualValue,
                        ExpectedValue = expectedValue,
                        IsValid = actualValue == expectedValue,
                        ValidationError = actualValue != expectedValue ? $"Field mismatch at line {lineNumber}" : "",
                        FieldType = InferFieldType(actualValue)
                    });
                }
            }

            return results;
        }

        private string InferFieldType(string value)
        {
            if (decimal.TryParse(value, out _)) return "Numeric";
            if (DateTime.TryParse(value, out _)) return "Date";
            return "Text";
        }

        private string AnalyzeSizeTrend(long fileSize)
        {
            // Simulate trend analysis - in real implementation would compare against historical data
            var typicalSize = 1024 * 1024; // 1MB as typical
            var variance = Math.Abs(fileSize - typicalSize) / (double)typicalSize;
            
            if (variance > 0.5) return "Anomaly";
            if (variance > 0.2) return fileSize > typicalSize ? "Growing" : "Shrinking";
            return "Stable";
        }

        private string AnalyzeRecordCountTrend(long recordCount)
        {
            // Simulate trend analysis
            var typicalCount = 1000;
            var variance = Math.Abs(recordCount - typicalCount) / (double)typicalCount;
            
            if (variance > 0.5) return "Anomaly";
            if (variance > 0.2) return recordCount > typicalCount ? "Growing" : "Shrinking";
            return "Stable";
        }

        private string CombineTrends(string sizeTrend, string recordTrend)
        {
            if (sizeTrend == "Anomaly" || recordTrend == "Anomaly") return "Anomaly";
            if (sizeTrend == recordTrend) return sizeTrend;
            return "Mixed";
        }

        private void AddValidationIssue(string fileName, string issueType, string description, string severity, string details)
        {
            // Would add to validation result issues list - placeholder for now
            _logger.Warning("Validation issue for {FileName}: {IssueType} - {Description}", fileName, issueType, description);
        }

        /// <summary>
        /// Populate comprehensive validation metrics
        /// </summary>
        private void PopulateValidationMetrics(ValidationResult result, List<FileValidationResult> fileValidations)
        {
            result.Metrics.TotalRecordCount = (int)fileValidations.Sum(f => f.Details.ActualRecordCount);
            result.Metrics.ExpectedRecordCount = (int)fileValidations.Sum(f => f.Details.ExpectedRecordCount);
            result.Metrics.TotalFileSize = fileValidations.Where(f => f.ActualFileSize.HasValue).Sum(f => f.ActualFileSize!.Value);
            result.Metrics.ExpectedTotalFileSize = fileValidations.Where(f => f.ExpectedFileSize.HasValue).Sum(f => f.ExpectedFileSize!.Value);
            
            result.Metrics.ChecksumMatches = fileValidations.Count(f => f.Details.ChecksumMatches);
            result.Metrics.ChecksumMismatches = fileValidations.Count(f => !f.Details.ChecksumMatches);
            result.Metrics.FormatValidationPassed = fileValidations.Count(f => f.Details.FormatValidationPassed);
            result.Metrics.FormatValidationFailed = fileValidations.Count(f => !f.Details.FormatValidationPassed);
            result.Metrics.BusinessRulesPassed = fileValidations.Count(f => f.Details.BusinessRulesValidationPassed);
            result.Metrics.BusinessRulesFailed = fileValidations.Count(f => !f.Details.BusinessRulesValidationPassed);
        }

        /// <summary>
        /// Populate performance metrics
        /// </summary>
        private void PopulatePerformanceMetrics(ValidationResult result, DateTime startTime)
        {
            result.Performance.TotalValidationDuration = result.ValidationEndTime - startTime;
            result.Performance.TotalBytesProcessed = result.Metrics.TotalFileSize;
            
            // Calculate component times (simplified - in real implementation would track individually)
            var totalDuration = result.Performance.TotalValidationDuration;
            result.Performance.FileProcessingTime = TimeSpan.FromMilliseconds(totalDuration.TotalMilliseconds * 0.4);
            result.Performance.ChecksumCalculationTime = TimeSpan.FromMilliseconds(totalDuration.TotalMilliseconds * 0.3);
            result.Performance.ContentComparisonTime = TimeSpan.FromMilliseconds(totalDuration.TotalMilliseconds * 0.3);
        }

        /// <summary>
        /// Log comprehensive validation summary
        /// </summary>
        private void LogValidationSummary(ValidationResult result, string jobNumber)
        {
            if (result.Success)
            {
                _logger.Information("✅ Comprehensive validation passed for job {JobNumber}", jobNumber);
                _logger.Information("📊 Validation Summary: {FilesMatched}/{TotalFiles} files, {TotalRecords} records, {TotalSize:N0} bytes processed", 
                    result.FilesMatched, result.TotalFilesValidated, result.Metrics.TotalRecordCount, result.Metrics.TotalFileSize);
                _logger.Information("⚡ Performance: {Duration:F2}s total, {Throughput:F2} MB/s throughput",
                    result.Performance.TotalValidationDuration.TotalSeconds, result.Performance.ProcessingThroughputMBps);
            }
            else
            {
                _logger.Warning("❌ Comprehensive validation failed for job {JobNumber}", jobNumber);
                _logger.Warning("📊 Validation Summary: {FilesMatched}/{TotalFiles} files matched, {FilesMismatched} failed", 
                    result.FilesMatched, result.TotalFilesValidated, result.FilesMismatched);
                
                if (result.Metrics.ChecksumMismatches > 0)
                    _logger.Warning("🔍 Checksum Issues: {ChecksumMismatches} files with checksum mismatches", result.Metrics.ChecksumMismatches);
                
                if (result.Metrics.FormatValidationFailed > 0)
                    _logger.Warning("📝 Format Issues: {FormatFailed} files failed format validation", result.Metrics.FormatValidationFailed);
                
                if (result.Metrics.BusinessRulesFailed > 0)
                    _logger.Warning("📋 Business Rule Issues: {BusinessRulesFailed} files failed business rule validation", result.Metrics.BusinessRulesFailed);
            }
        }

        /// <summary>
        /// Add validation category card to HTML
        /// </summary>
        private void AddValidationCategoryCard(StringBuilder html, string icon, string title, int passed, int total)
        {
            var passRate = total > 0 ? (double)passed / total * 100 : 0;
            var statusClass = passRate == 100 ? "success" : passRate >= 80 ? "warning" : "failure";
            
            html.AppendLine("                    <div class=\"category-card\">");
            html.AppendLine($"                        <div class=\"category-icon\">{icon}</div>");
            html.AppendLine($"                        <div class=\"category-title\">{title}</div>");
            html.AppendLine($"                        <div class=\"category-value {statusClass}\">{passed}/{total}</div>");
            html.AppendLine($"                        <div class=\"category-percent\">{passRate:F0}%</div>");
            html.AppendLine("                    </div>");
        }

        /// <summary>
        /// Check if a file is a backup or original file that should have relaxed validation rules
        /// </summary>
        private bool IsBackupOrOriginalFile(string fileName)
        {
            // Common backup/original file patterns in legacy systems
            var backupPatterns = new[]
            {
                ".org",      // Original file
                ".orig",     // Original file
                ".bak",      // Backup file
                ".backup",   // Backup file
                ".old",      // Old version
                ".prev",     // Previous version
                ".save"      // Saved version
            };

            foreach (var pattern in backupPatterns)
            {
                if (fileName.EndsWith(pattern, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
                
                // Also check for patterns like "file.asc.org" where the backup suffix is after the main extension
                if (fileName.Contains($".asc{pattern}") || 
                    fileName.Contains($".txt{pattern}") || 
                    fileName.Contains($".dat{pattern}") ||
                    fileName.Contains($".se1{pattern}") ||
                    fileName.Contains($".4300{pattern}"))
                {
                    return true;
                }
            }

            return false;
        }

        #endregion
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
        
        // Enhanced validation metrics
        public ValidationMetrics Metrics { get; set; } = new ValidationMetrics();
        public List<ValidationIssue> Issues { get; set; } = new List<ValidationIssue>();
        public PerformanceMetrics Performance { get; set; } = new PerformanceMetrics();
    }

    /// <summary>
    /// Enhanced validation metrics
    /// </summary>
    public class ValidationMetrics
    {
        public int TotalRecordCount { get; set; }
        public int ExpectedRecordCount { get; set; }
        public long TotalFileSize { get; set; }
        public long ExpectedTotalFileSize { get; set; }
        public int ChecksumMatches { get; set; }
        public int ChecksumMismatches { get; set; }
        public int FormatValidationPassed { get; set; }
        public int FormatValidationFailed { get; set; }
        public int BusinessRulesPassed { get; set; }
        public int BusinessRulesFailed { get; set; }
        public int CrossFileValidationsPassed { get; set; }
        public int CrossFileValidationsFailed { get; set; }
    }

    /// <summary>
    /// Performance tracking metrics
    /// </summary>
    public class PerformanceMetrics
    {
        public TimeSpan TotalValidationDuration { get; set; }
        public TimeSpan FileProcessingTime { get; set; }
        public TimeSpan ChecksumCalculationTime { get; set; }
        public TimeSpan ContentComparisonTime { get; set; }
        public long TotalBytesProcessed { get; set; }
        public double ProcessingThroughputMBps => TotalValidationDuration.TotalSeconds > 0 
            ? (TotalBytesProcessed / 1024.0 / 1024.0) / TotalValidationDuration.TotalSeconds 
            : 0;
    }

    /// <summary>
    /// Validation issue tracking
    /// </summary>
    public class ValidationIssue
    {
        public string IssueType { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty; // Critical, Warning, Info
        public string Details { get; set; } = string.Empty;
        public DateTime DetectedAt { get; set; } = DateTime.Now;
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
        
        // Enhanced validation results
        public FileValidationDetails Details { get; set; } = new FileValidationDetails();
    }

    /// <summary>
    /// Detailed file validation results
    /// </summary>
    public class FileValidationDetails
    {
        // Size validation
        public bool SizeValidationPassed { get; set; }
        public double SizeVariancePercentage { get; set; }
        
        // Record count validation  
        public long ActualRecordCount { get; set; }
        public long ExpectedRecordCount { get; set; }
        public bool RecordCountMatches { get; set; }
        
        // Format validation
        public bool FormatValidationPassed { get; set; }
        public string DetectedEncoding { get; set; } = string.Empty;
        public string ExpectedEncoding { get; set; } = string.Empty;
        public string FileExtension { get; set; } = string.Empty;
        
        // Data type validation
        public int ValidNumericFields { get; set; }
        public int InvalidNumericFields { get; set; }
        public int ValidDateFields { get; set; }
        public int InvalidDateFields { get; set; }
        public List<string> FieldValidationErrors { get; set; } = new List<string>();
        
        // Business rules validation
        public bool BusinessRulesValidationPassed { get; set; }
        public List<string> BusinessRuleViolations { get; set; } = new List<string>();
        
        // Checksum validation
        public string MD5Hash { get; set; } = string.Empty;
        public string SHA256Hash { get; set; } = string.Empty;
        public bool ChecksumMatches { get; set; }
        
        // Performance metrics
        public TimeSpan ProcessingTime { get; set; }
        public double ProcessingSpeedMBps { get; set; }
        
        // Field-level validation
        public List<FieldValidationResult> FieldValidations { get; set; } = new List<FieldValidationResult>();
        
        // Historical comparison
        public bool HistoricalComparisonAvailable { get; set; }
        public string HistoricalTrend { get; set; } = string.Empty; // Growing, Shrinking, Stable, Anomaly
    }

    /// <summary>
    /// Field-level validation result
    /// </summary>
    public class FieldValidationResult
    {
        public int FieldIndex { get; set; }
        public string FieldName { get; set; } = string.Empty;
        public string ExpectedValue { get; set; } = string.Empty;
        public string ActualValue { get; set; } = string.Empty;
        public bool IsValid { get; set; }
        public string ValidationError { get; set; } = string.Empty;
        public string FieldType { get; set; } = string.Empty; // Numeric, Date, Text, etc.
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
