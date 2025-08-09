using System;
using System.IO;
using System.Threading.Tasks;
using Serilog;
using LegacyModernization.Core.Configuration;
using LegacyModernization.Core.Validation;
using LegacyModernization.Core.Components;

namespace LegacyModernization.Validation
{
    /// <summary>
    /// Validation Runner for Testing Pipeline Output
    /// Task 3.2 - Comprehensive Output Validation & Testing
    /// Enhanced with TwoStageValidationComponent for parallel processing validation (Container Step 1 || MB2000 Conversion)
    /// Supports record-level and field-level analysis for both parallel stages
    /// </summary>
    class ValidationRunner
    {
        /// <summary>
        /// Generate enhanced validation report from TwoStageValidationResult for parallel processing architecture
        /// </summary>
        private static string GenerateEnhancedValidationReport(TwoStageValidationResult result, string jobNumber, string validationLevel)
        {
            var report = new System.Text.StringBuilder();
            
            report.AppendLine("==================================================");
            report.AppendLine("        ENHANCED PARALLEL PIPELINE VALIDATION REPORT");
            report.AppendLine("==================================================");
            report.AppendLine($"Job Number: {jobNumber}");
            report.AppendLine($"Validation Level: {validationLevel.ToUpper()}");
            report.AppendLine($"Validation Time: {result.ValidationStartTime:yyyy-MM-dd HH:mm:ss}");
            report.AppendLine($"Duration: {result.ValidationDuration.TotalSeconds:F3}s");
            report.AppendLine($"Overall Result: {(result.Success ? "✅ PASSED" : "❌ FAILED")}");
            report.AppendLine($"Overall Accuracy: {result.OverallAccuracy:F2}%");
            report.AppendLine();
            
            report.AppendLine("STAGE VALIDATION DETAILS:");
            report.AppendLine("--------------------------------------------------");
            
            // Stage 1 - Binary to Container (Container Step 1)
            report.AppendLine($"🔄 Stage 1 - Container Step 1 (Binary→Container):");
            report.AppendLine($"     Status: {(result.Stage1Validation.Success ? "✅ PASS" : "❌ FAIL")}");
            report.AppendLine($"     Accuracy: {result.Stage1Validation.AccuracyPercentage:F2}%");
            if (result.Stage1Validation.ExpectedSize > 0)
            {
                report.AppendLine($"     Size: {result.Stage1Validation.ActualSize}/{result.Stage1Validation.ExpectedSize} bytes");
            }
            if (result.Stage1Validation.ExpectedRecordCount.HasValue && validationLevel != "basic")
            {
                report.AppendLine($"     Records: {result.Stage1Validation.ActualRecordCount}/{result.Stage1Validation.ExpectedRecordCount}");
                report.AppendLine($"     Record Accuracy: {result.Stage1Validation.RecordLevelAccuracy:F2}%");
            }
            if (result.Stage1Validation.CorrectFields.HasValue && validationLevel == "comprehensive")
            {
                report.AppendLine($"     Field Accuracy: {result.Stage1Validation.FieldLevelAccuracy:F2}%");
            }
            report.AppendLine();
            
            // Stage 2 - Binary to MB2000 (Parallel Processing)
            report.AppendLine($"🔄 Stage 2 - MB2000 Conversion (Binary→MB2000):");
            report.AppendLine($"     Status: {(result.Stage2Validation.Success ? "✅ PASS" : "❌ FAIL")}");
            report.AppendLine($"     Accuracy: {result.Stage2Validation.AccuracyPercentage:F2}%");
            if (result.Stage2Validation.ExpectedSize > 0)
            {
                report.AppendLine($"     Size: {result.Stage2Validation.ActualSize}/{result.Stage2Validation.ExpectedSize} bytes");
            }
            if (result.Stage2Validation.ExpectedRecordCount.HasValue && validationLevel != "basic")
            {
                report.AppendLine($"     Records: {result.Stage2Validation.ActualRecordCount}/{result.Stage2Validation.ExpectedRecordCount}");
                report.AppendLine($"     Record Accuracy: {result.Stage2Validation.RecordLevelAccuracy:F2}%");
            }
            if (result.Stage2Validation.CorrectFields.HasValue && validationLevel == "comprehensive")
            {
                report.AppendLine($"     Field Accuracy: {result.Stage2Validation.FieldLevelAccuracy:F2}%");
            }
            report.AppendLine();
            
            // Overall Pipeline
            report.AppendLine($"🔄 Overall Pipeline Validation:");
            report.AppendLine($"     Status: {(result.OverallValidation.Success ? "✅ PASS" : "❌ FAIL")}");
            report.AppendLine($"     Accuracy: {result.OverallValidation.AccuracyPercentage:F2}%");
            if (result.OverallValidation.ExpectedSize > 0)
            {
                report.AppendLine($"     Size: {result.OverallValidation.ActualSize}/{result.OverallValidation.ExpectedSize} bytes");
            }
            report.AppendLine();
            
            // Enhanced Analysis (if available)
            if (result.RecordLevelValidation != null)
            {
                report.AppendLine("RECORD-LEVEL ANALYSIS:");
                report.AppendLine("--------------------------------------------------");
                report.AppendLine($"📊 Record Analysis Status: {(result.RecordLevelValidation.Success ? "✅ PASS" : "❌ FAIL")}");
                report.AppendLine($"📊 Record Accuracy: {result.RecordLevelValidation.AccuracyPercentage:F2}%");
                if (result.RecordLevelValidation.CorrectRecords.HasValue)
                {
                    report.AppendLine($"📊 Records Correct: {result.RecordLevelValidation.CorrectRecords}/{result.RecordLevelValidation.TotalRecords}");
                }
                report.AppendLine();
            }
            
            if (result.FieldLevelValidation != null)
            {
                report.AppendLine("FIELD-LEVEL ANALYSIS:");
                report.AppendLine("--------------------------------------------------");
                report.AppendLine($"🔬 Field Analysis Status: {(result.FieldLevelValidation.Success ? "✅ PASS" : "❌ FAIL")}");
                report.AppendLine($"🔬 Field Accuracy: {result.FieldLevelValidation.AccuracyPercentage:F2}%");
                if (result.FieldLevelValidation.CorrectFields.HasValue)
                {
                    report.AppendLine($"🔬 Fields Correct: {result.FieldLevelValidation.CorrectFields}/{result.FieldLevelValidation.TotalFields}");
                }
                report.AppendLine();
            }
            
            // Validation Messages
            var allMessages = new List<string>();
            allMessages.AddRange(result.Stage1Validation.ValidationMessages);
            allMessages.AddRange(result.Stage2Validation.ValidationMessages);
            allMessages.AddRange(result.OverallValidation.ValidationMessages);
            
            if (allMessages.Count > 0)
            {
                report.AppendLine("VALIDATION MESSAGES:");
                report.AppendLine("--------------------------------------------------");
                foreach (var message in allMessages)
                {
                    report.AppendLine($"• {message}");
                }
                report.AppendLine();
            }
            
            report.AppendLine("RECOMMENDATIONS:");
            report.AppendLine("--------------------------------------------------");
            if (result.Success)
            {
                report.AppendLine("✅ All validations passed successfully!");
                report.AppendLine("• Pipeline is producing correct output with expected accuracy");
                report.AppendLine("• All file sizes and checksums match expected values");
                if (validationLevel == "comprehensive")
                {
                    report.AppendLine("• Record-level and field-level analysis completed successfully");
                }
            }
            else
            {
                report.AppendLine("❌ Some validations failed. Investigate the following:");
                if (!result.Stage1Validation.Success)
                {
                    report.AppendLine("• Container Step 1 processing issues detected");
                }
                if (!result.Stage2Validation.Success)
                {
                    report.AppendLine("• MB2000 conversion processing issues detected");
                }
                if (!result.OverallValidation.Success)
                {
                    report.AppendLine("• Overall pipeline output doesn't match expected results");
                }
            }
            
            report.AppendLine("==================================================");
            
            return report.ToString();
        }
        static async Task<int> Main(string[] args)
        {
            try
            {
                // Configure logging
                Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.Information()
                    .WriteTo.Console()
                    .WriteTo.File("validation_results.log")
                    .CreateLogger();

                var logger = Log.Logger;

                logger.Information("Starting Legacy Modernization Output Validation");

                // Validate command line arguments
                if (args.Length < 1)
                {
                    logger.Error("Usage: ValidationRunner <job_number> [expected_output_path] [--format text|html] [--enhanced] [--level basic|detailed|comprehensive]");
                    logger.Information("Examples:");
                    logger.Information("  ValidationRunner 69172");
                    logger.Information("  ValidationRunner 69172 --format html");
                    logger.Information("  ValidationRunner 69172 --enhanced --level comprehensive");
                    logger.Information("  ValidationRunner 69172 C:\\Expected\\Output --enhanced --level detailed");
                    return 1;
                }

                var jobNumber = args[0];
                var expectedOutputPath = @"c:\Users\Shan\Documents\Legacy Mordernization\MBCNTR2053_Expected_Output";
                var outputFormat = "text"; // Default format
                var useEnhancedValidation = false;
                var validationLevel = "basic"; // Default level

                // Parse additional arguments
                for (int i = 1; i < args.Length; i++)
                {
                    if (args[i] == "--format" && i + 1 < args.Length)
                    {
                        outputFormat = args[i + 1].ToLowerInvariant();
                        i++; // Skip the format value
                    }
                    else if (args[i] == "--level" && i + 1 < args.Length)
                    {
                        validationLevel = args[i + 1].ToLowerInvariant();
                        i++; // Skip the level value
                    }
                    else if (args[i] == "--enhanced")
                    {
                        useEnhancedValidation = true;
                    }
                    else if (!args[i].StartsWith("--"))
                    {
                        // Assume it's the expected output path
                        expectedOutputPath = args[i];
                    }
                }

                // Validate format
                if (outputFormat != "text" && outputFormat != "html")
                {
                    logger.Error("Invalid format: {Format}. Valid formats are: text, html", outputFormat);
                    return 1;
                }

                // Validate validation level
                if (validationLevel != "basic" && validationLevel != "detailed" && validationLevel != "comprehensive")
                {
                    logger.Error("Invalid validation level: {Level}. Valid levels are: basic, detailed, comprehensive", validationLevel);
                    return 1;
                }

                logger.Information("Validation Parameters:");
                logger.Information("  Job Number: {JobNumber}", jobNumber);
                logger.Information("  Expected Output Path: {ExpectedPath}", expectedOutputPath);
                logger.Information("  Output Format: {Format}", outputFormat);
                logger.Information("  Enhanced Validation: {Enhanced}", useEnhancedValidation);
                logger.Information("  Validation Level: {Level}", validationLevel);

                // Validate paths exist
                if (!Directory.Exists(expectedOutputPath))
                {
                    logger.Error("Expected output path does not exist: {ExpectedPath}", expectedOutputPath);
                    return 1;
                }

                // Setup configuration
                var projectBase = @"c:\Users\Shan\Documents\Legacy Mordernization\LegacyModernizationPoC";
                var configuration = new PipelineConfiguration
                {
                    ProjectBase = projectBase,
                    OutputPath = Path.Combine(projectBase, "Output"),
                    LogPath = Path.Combine(projectBase, "Logs"),
                    InputPath = Path.Combine(projectBase, "TestData")
                };

                logger.Information("Pipeline Configuration:");
                logger.Information("  Project Base: {ProjectBase}", configuration.ProjectBase);
                logger.Information("  Actual Output Path: {ActualPath}", configuration.OutputPath);

                // Validate actual output path exists
                if (!Directory.Exists(configuration.OutputPath))
                {
                    logger.Error("Actual output path does not exist: {ActualPath}", configuration.OutputPath);
                    return 1;
                }

                // Execute validation
                logger.Information("Executing comprehensive output validation...");
                
                if (useEnhancedValidation)
                {
                    // Use enhanced TwoStageValidationComponent
                    logger.Information("🚀 Using Enhanced Parallel Pipeline Validation with {Level} analysis", validationLevel.ToUpper());
                    
                    var enhancedValidator = new TwoStageValidationComponent(logger);
                    
                    // Parse validation level
                    ValidationLevel level = validationLevel switch
                    {
                        "basic" => ValidationLevel.Basic,
                        "detailed" => ValidationLevel.Detailed,
                        "comprehensive" => ValidationLevel.Comprehensive,
                        _ => ValidationLevel.Basic
                    };
                    
                    // Build file paths for enhanced validation
                    var binaryInputPath = Path.Combine(configuration.InputPath, $"{jobNumber}.dat");
                    var containerOutputPath = Path.Combine(configuration.OutputPath, $"{jobNumber}.4300");
                    var mb2000OutputPath = Path.Combine(configuration.OutputPath, $"{jobNumber}p.asc");
                    var expectedContainerPath = Path.Combine(expectedOutputPath, $"{jobNumber}.4300");
                    var expectedMb2000Path = Path.Combine(expectedOutputPath, $"{jobNumber}p.asc");
                    
                    // Run enhanced validation for parallel processing architecture
                    var enhancedResult = await enhancedValidator.ValidateParallelPipelineAsync(
                        binaryInputPath,
                        containerOutputPath,
                        mb2000OutputPath, 
                        expectedContainerPath,
                        expectedMb2000Path, 
                        level);
                    
                    // Display enhanced validation results
                    logger.Information("=== Enhanced Validation Results ===");
                    logger.Information(enhancedResult.GetValidationSummary());
                    
                    // Generate enhanced validation report
                    string enhancedReport = GenerateEnhancedValidationReport(enhancedResult, jobNumber, validationLevel);
                    Console.WriteLine(enhancedReport);
                    
                    // Write enhanced report to file
                    var enhancedReportPath = Path.Combine(configuration.LogPath, $"enhanced_validation_report_{jobNumber}_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
                    Directory.CreateDirectory(Path.GetDirectoryName(enhancedReportPath)!);
                    await File.WriteAllTextAsync(enhancedReportPath, enhancedReport);
                    logger.Information("Enhanced validation report written to: {ReportPath}", enhancedReportPath);
                    
                    // Return result
                    if (enhancedResult.Success)
                    {
                        logger.Information("✅ Enhanced validation passed successfully!");
                        return 0;
                    }
                    else
                    {
                        logger.Warning("❌ Enhanced validation failed. See report for details.");
                        return 1;
                    }
                }
                else
                {
                    // Use existing OutputValidator
                    logger.Information("Using standard output validation");
                    var validator = new OutputValidator(logger, configuration);
                    var validationResult = await validator.ValidateOutputAsync(jobNumber, expectedOutputPath);

                    // Generate and display report
                    string report;
                    string reportPath;
                    
                    if (outputFormat == "html")
                    {
                        // Generate HTML report
                        var htmlReport = validator.GenerateHtmlValidationReport(validationResult);
                        reportPath = Path.Combine(configuration.LogPath, $"validation_report_{jobNumber}_{DateTime.Now:yyyyMMdd_HHmmss}.html");
                        Directory.CreateDirectory(Path.GetDirectoryName(reportPath)!);
                        await File.WriteAllTextAsync(reportPath, htmlReport);
                        logger.Information("HTML validation report written to: {ReportPath}", reportPath);
                        
                        // Also generate text for console display
                        report = validator.GenerateValidationReport(validationResult);
                        Console.WriteLine(report);
                    }
                    else
                    {
                        // Generate text report
                        report = validator.GenerateValidationReport(validationResult);
                        Console.WriteLine(report);

                        // Write report to file
                        reportPath = Path.Combine(configuration.LogPath, $"validation_report_{jobNumber}_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
                        Directory.CreateDirectory(Path.GetDirectoryName(reportPath)!);
                        await File.WriteAllTextAsync(reportPath, report);
                        logger.Information("Text validation report written to: {ReportPath}", reportPath);
                    }

                    // Return appropriate exit code
                    if (validationResult.Success)
                    {
                        logger.Information("✅ All validations passed successfully!");
                        return 0;
                    }
                    else
                    {
                        logger.Warning("❌ Some validations failed. See report for details.");
                        return 1;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Logger?.Error(ex, "Validation runner failed with exception");
                Console.WriteLine($"Fatal error: {ex.Message}");
                return 1;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}
