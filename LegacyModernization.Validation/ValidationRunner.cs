using System;
using System.IO;
using System.Threading.Tasks;
using Serilog;
using LegacyModernization.Core.Configuration;
using LegacyModernization.Core.Validation;

namespace LegacyModernization.Validation
{
    /// <summary>
    /// Validation Runner for Testing Pipeline Output
    /// Task 3.2 - Comprehensive Output Validation & Testing
    /// </summary>
    class ValidationRunner
    {
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
                    logger.Error("Usage: ValidationRunner <job_number> [expected_output_path] [--format text|html]");
                    logger.Information("Examples:");
                    logger.Information("  ValidationRunner 69172");
                    logger.Information("  ValidationRunner 69172 --format html");
                    logger.Information("  ValidationRunner 69172 C:\\Expected\\Output --format html");
                    return 1;
                }

                var jobNumber = args[0];
                var expectedOutputPath = @"c:\Users\Shan\Documents\Legacy Mordernization\MBCNTR2053_Expected_Output";
                var outputFormat = "text"; // Default format

                // Parse additional arguments
                for (int i = 1; i < args.Length; i++)
                {
                    if (args[i] == "--format" && i + 1 < args.Length)
                    {
                        outputFormat = args[i + 1].ToLowerInvariant();
                        i++; // Skip the format value
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

                logger.Information("Validation Parameters:");
                logger.Information("  Job Number: {JobNumber}", jobNumber);
                logger.Information("  Expected Output Path: {ExpectedPath}", expectedOutputPath);
                logger.Information("  Output Format: {Format}", outputFormat);

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

                // Create output validator
                var validator = new OutputValidator(logger, configuration);

                // Execute validation
                logger.Information("Executing comprehensive output validation...");
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
