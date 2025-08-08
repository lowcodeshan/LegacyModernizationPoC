using LegacyModernization.Core.Configuration;
using LegacyModernization.Core.Logging;
using LegacyModernization.Core.Pipeline;
using LegacyModernization.Core.Models;
using Serilog;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace LegacyModernization.Pipeline
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            var startTime = DateTime.Now;
            
            // Initialize configuration
            var projectBase = Path.GetDirectoryName(AppContext.BaseDirectory) 
                ?? throw new InvalidOperationException("Could not determine project base directory");
            
            // Navigate up to solution root (typically 4 levels: bin/Debug/net8.0/LegacyModernization.Pipeline -> project root)
            var solutionRoot = GetSolutionRoot(projectBase);
            var config = PipelineConfiguration.CreateDefault(solutionRoot);

            // Create logger
            var logger = Core.Logging.LoggerConfiguration.CreateLogger(config.LogPath, "pipeline");
            
            // Create progress reporter
            var progressReporter = new ProgressReporter(logger, false);
            
            try
            {
                // Display startup banner with timestamp (equivalent to legacy script banner and date)
                progressReporter.DisplayStartupBanner();
                
                // Parse arguments
                var arguments = ParseArguments(args);
                if (arguments == null)
                {
                    DisplayUsage();
                    return 1;
                }

                // Update progress reporter verbosity based on arguments
                progressReporter = new ProgressReporter(logger, arguments.Verbose);

                // Enhanced argument validation using ArgumentValidator
                var validationResult = ArgumentValidator.ValidateArguments(arguments);
                if (!validationResult.IsValid)
                {
                    logger.Error("Argument validation failed: {ErrorMessage}", validationResult.ErrorMessage);
                    Console.WriteLine($"Error: {validationResult.ErrorMessage}");
                    DisplayUsage();
                    return 1;
                }

                logger.Information("Starting Legacy Modernization Pipeline for job {JobNumber}", arguments.JobNumber);
                progressReporter.LogConfiguration(config);

                // Initialize pipeline progress tracking
                progressReporter.InitializeProgress(6); // 6 major steps in the full pipeline

                // Execute Task 2.1: Parameter Validation & Environment Setup Component
                var containerComponent = new ContainerParameterValidationComponent(logger, progressReporter, config);
                var validationSuccess = await containerComponent.ExecuteAsync(arguments);

                if (!validationSuccess)
                {
                    logger.Error("Parameter validation and environment setup failed");
                    var duration = DateTime.Now - startTime;
                    progressReporter.ReportCompletion(false, duration);
                    return 1;
                }

                // Execute Task 2.2: Supplemental File Processing Component
                var supplementalComponent = new SupplementalFileProcessingComponent(logger, progressReporter, config);
                var supplementalSuccess = await supplementalComponent.ExecuteAsync(arguments);

                if (!supplementalSuccess)
                {
                    logger.Error("Supplemental file processing failed");
                    var duration = DateTime.Now - startTime;
                    progressReporter.ReportCompletion(false, duration);
                    return 1;
                }

                // Dry run mode check
                if (arguments.DryRun)
                {
                    logger.Information("Dry run completed successfully - validation passed");
                    Console.WriteLine("✓ Dry run completed successfully - all validations passed");
                    var dryRunDuration = DateTime.Now - startTime;
                    progressReporter.ReportCompletion(true, dryRunDuration);
                    return 0;
                }

                // TODO: Execute remaining pipeline steps (Tasks 2.4-2.5)
                
                // Task 2.3: Container Step 1 Implementation - ncpcntr5v2.script equivalent
                try
                {
                    var containerStep1Component = new ContainerStep1Component(logger, progressReporter, config);
                    var containerResult = await containerStep1Component.ExecuteAsync(arguments);
                    
                    if (!containerResult)
                    {
                        logger.Error("Container Step 1 processing failed");
                        throw new InvalidOperationException("Container processing failed");
                    }
                    
                    logger.Information("Container Step 1 completed successfully");
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Container Step 1 processing failed");
                    throw;
                }
                
                // Task 2.4: MB2000 Conversion Implementation - setmb2000.script equivalent
                try
                {
                    var mb2000ConversionComponent = new MB2000ConversionComponent(logger, progressReporter, config);
                    var conversionResult = await mb2000ConversionComponent.ExecuteAsync(arguments);
                    
                    if (!conversionResult)
                    {
                        logger.Error("MB2000 conversion processing failed");
                        throw new InvalidOperationException("MB2000 conversion failed");
                    }
                    
                    logger.Information("MB2000 conversion completed successfully");
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "MB2000 conversion processing failed");
                    throw;
                }

                // Task 2.5: E-bill Split Processing Implementation - cnpsplit4.out + mv operations equivalent
                try
                {
                    var ebillSplitComponent = new EbillSplitComponent(logger, progressReporter, config);
                    var splitResult = await ebillSplitComponent.ExecuteAsync(arguments);
                    
                    if (!splitResult)
                    {
                        logger.Error("E-bill split processing failed");
                        throw new InvalidOperationException("E-bill split processing failed");
                    }
                    
                    logger.Information("E-bill split processing completed successfully");
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "E-bill split processing failed");
                    throw;
                }

                progressReporter.ReportStep("Pipeline Integration", "All core pipeline components completed successfully");

                logger.Information("Tasks 2.1-2.2 - Parameter Validation, Environment Setup, and Supplemental File Processing completed successfully");
                var totalDuration = DateTime.Now - startTime;
                progressReporter.ReportCompletion(true, totalDuration);
                return 0;

            }
            catch (Exception ex)
            {
                logger.Error(ex, "Fatal error during pipeline execution");
                Console.WriteLine($"Fatal error: {ex.Message}");
                var errorDuration = DateTime.Now - startTime;
                progressReporter.ReportCompletion(false, errorDuration);
                return 1;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        private static PipelineArguments? ParseArguments(string[] args)
        {
            if (args.Length == 0)
                return null;

            var arguments = new PipelineArguments();

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i].ToLower())
                {
                    case "-h":
                    case "--help":
                        return null;
                    
                    case "-v":
                    case "--verbose":
                        arguments.Verbose = true;
                        arguments.LogLevel = "Debug";
                        break;
                    
                    case "-d":
                    case "--dry-run":
                        arguments.DryRun = true;
                        break;
                    
                    case "-s":
                    case "--source-file":
                        if (i + 1 < args.Length)
                        {
                            arguments.SourceFilePath = args[++i];
                        }
                        break;
                    
                    case "-l":
                    case "--log-level":
                        if (i + 1 < args.Length)
                        {
                            arguments.LogLevel = args[++i];
                        }
                        break;
                    
                    default:
                        // Assume first non-option argument is job number
                        if (string.IsNullOrEmpty(arguments.JobNumber) && !args[i].StartsWith("-"))
                        {
                            arguments.JobNumber = args[i];
                        }
                        break;
                }
            }

            return arguments;
        }

        private static void DisplayUsage()
        {
            Console.WriteLine(PipelineArguments.GetUsage());
        }

        private static string GetSolutionRoot(string startPath)
        {
            var current = new DirectoryInfo(startPath);
            
            // Look for solution file or specific project structure
            while (current != null)
            {
                if (File.Exists(Path.Combine(current.FullName, "LegacyModernization.sln")) ||
                    Directory.Exists(Path.Combine(current.FullName, "LegacyModernization.Core")))
                {
                    return current.FullName;
                }
                current = current.Parent;
            }
            
            // Fallback to a subdirectory of the current path
            return Path.Combine(startPath, "LegacyModernizationOutput");
        }
    }
}
