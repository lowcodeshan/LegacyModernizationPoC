using LegacyModernization.Core.Configuration;
using LegacyModernization.Core.Logging;
using Serilog;
using System;
using System.IO;
using System.Threading.Tasks;

namespace LegacyModernization.Core.Pipeline
{
    /// <summary>
    /// Container Step 1: Parameter Validation & Environment Setup Component
    /// Implements the initial parameter validation and environment configuration 
    /// equivalent to lines 1-30 of mbcntr2503.script
    /// </summary>
    public class ContainerParameterValidationComponent
    {
        private readonly ILogger _logger;
        private readonly ProgressReporter _progressReporter;
        private readonly PipelineConfiguration _configuration;

        public ContainerParameterValidationComponent(
            ILogger logger, 
            ProgressReporter progressReporter,
            PipelineConfiguration configuration)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _progressReporter = progressReporter ?? throw new ArgumentNullException(nameof(progressReporter));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        /// <summary>
        /// Execute parameter validation and environment setup
        /// </summary>
        /// <param name="arguments">Pipeline arguments</param>
        /// <returns>Task representing the async operation with success status</returns>
        public async Task<bool> ExecuteAsync(PipelineArguments arguments)
        {
            try
            {
                _progressReporter.ReportStep("Parameter Validation", "Starting validation", false);

                // Step 1: Validate command-line parameters
                if (!await ValidateCommandLineParametersAsync(arguments))
                {
                    return false;
                }

                // Step 2: Setup environment variables and configuration
                if (!await SetupEnvironmentAsync(arguments))
                {
                    return false;
                }

                // Step 3: Initialize logging and banner (already done in main, but verify)
                if (!await InitializeLoggingSystemAsync(arguments))
                {
                    return false;
                }

                // Step 4: Validate file paths and permissions
                if (!await ValidateFileSystemAsync(arguments))
                {
                    return false;
                }

                _progressReporter.ReportStep("Parameter Validation & Environment Setup", 
                    $"Job {arguments.JobNumber} validated and environment configured", true);

                return true;
            }
            catch (Exception ex)
            {
                _progressReporter.ReportStepError("Parameter Validation", ex.Message, ex);
                return false;
            }
        }

        /// <summary>
        /// Validate command-line parameters equivalent to $1 and $2 validation in legacy script
        /// </summary>
        /// <param name="arguments">Pipeline arguments</param>
        /// <returns>True if validation passes</returns>
        private async Task<bool> ValidateCommandLineParametersAsync(PipelineArguments arguments)
        {
            await Task.Delay(10); // Simulate async work

            _logger.Information("Validating command-line parameters");

            // Comprehensive argument validation using ArgumentValidator
            var validationResult = ArgumentValidator.ValidateArguments(arguments);
            if (!validationResult.IsValid)
            {
                _progressReporter.ReportStepError("Parameter Validation", validationResult.ErrorMessage);
                _logger.Error("Argument validation failed: {ErrorMessage}", validationResult.ErrorMessage);
                return false;
            }

            // Additional specific validations
            _logger.Information("Job number validation passed: {JobNumber}", arguments.JobNumber);

            // Validate source file parameter if provided
            if (!string.IsNullOrEmpty(arguments.SourceFilePath))
            {
                var sourceValidation = ArgumentValidator.ValidateSourceFile(arguments.SourceFilePath);
                if (!sourceValidation.IsValid)
                {
                    _progressReporter.ReportStepError("Source File Validation", sourceValidation.ErrorMessage);
                    return false;
                }
                _logger.Information("Source file validation passed: {SourceFile}", arguments.SourceFilePath);
            }
            else
            {
                // Use default input file path based on job number
                var defaultInputPath = _configuration.GetInputFilePath(arguments.JobNumber);
                arguments.SourceFilePath = defaultInputPath;
                _logger.Information("Using default input file path: {InputPath}", defaultInputPath);
            }

            _progressReporter.ReportStepCompleted("Command-line Parameter Validation");
            return true;
        }

        /// <summary>
        /// Setup environment variables and configuration equivalent to mbcntr2503.script environment setup
        /// </summary>
        /// <param name="arguments">Pipeline arguments</param>
        /// <returns>True if setup successful</returns>
        private async Task<bool> SetupEnvironmentAsync(PipelineArguments arguments)
        {
            await Task.Delay(10); // Simulate async work

            _logger.Information("Setting up environment configuration");

            try
            {
                // Validate environment configuration
                var envValidation = ArgumentValidator.ValidateEnvironment(_configuration);
                if (!envValidation.IsValid)
                {
                    _progressReporter.ReportStepError("Environment Setup", envValidation.ErrorMessage);
                    return false;
                }

                // Log configuration constants (equivalent to script variable assignments)
                _logger.Information("Environment Configuration:");
                _logger.Information("  ClientDept: {ClientDept}", PipelineConfiguration.ClientDept);
                _logger.Information("  ServiceType: {ServiceType}", PipelineConfiguration.ServiceType);
                _logger.Information("  ContainerKey: {ContainerKey}", PipelineConfiguration.ContainerKey);
                _logger.Information("  OptionLength: {OptionLength}", PipelineConfiguration.OptionLength);
                _logger.Information("  Work2Length: {Work2Length}", PipelineConfiguration.Work2Length);
                _logger.Information("  ProjectType: {ProjectType}", PipelineConfiguration.ProjectType);

                // Setup file paths
                var inputPath = _configuration.GetInputFilePath(arguments.JobNumber);
                var outputPath = _configuration.GetPaperBillOutputPath(arguments.JobNumber);
                var supplementalPath = _configuration.GetSupplementalTablePath(arguments.JobNumber);

                _logger.Information("File Paths:");
                _logger.Information("  Input Path: {InputPath}", inputPath);
                _logger.Information("  Output Path: {OutputPath}", outputPath);
                _logger.Information("  Supplemental Path: {SupplementalPath}", supplementalPath);

                // Ensure output directories exist
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
                Directory.CreateDirectory(Path.GetDirectoryName(supplementalPath)!);

                _progressReporter.ReportStepCompleted("Environment Setup", 
                    $"Configuration validated, paths initialized");

                return true;
            }
            catch (Exception ex)
            {
                _progressReporter.ReportStepError("Environment Setup", $"Failed to setup environment: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// Initialize logging system with proper configuration
        /// </summary>
        /// <param name="arguments">Pipeline arguments</param>
        /// <returns>True if initialization successful</returns>
        private async Task<bool> InitializeLoggingSystemAsync(PipelineArguments arguments)
        {
            await Task.Delay(10); // Simulate async work

            try
            {
                // Log the pipeline configuration for debugging
                _progressReporter.LogConfiguration(_configuration);

                // Set up structured logging for pipeline execution tracking
                _logger.Information("Logging system initialized successfully");
                _logger.Information("Log level set to: {LogLevel}", arguments.LogLevel);

                if (arguments.Verbose)
                {
                    _logger.Information("Verbose logging enabled");
                }

                if (arguments.DryRun)
                {
                    _logger.Information("Dry run mode enabled - no actual processing will occur");
                    _progressReporter.ReportStepWarning("Logging Initialization", "Dry run mode active");
                }

                _progressReporter.ReportStepCompleted("Logging System Initialization");
                return true;
            }
            catch (Exception ex)
            {
                _progressReporter.ReportStepError("Logging Initialization", $"Failed to initialize logging: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// Validate file system access and permissions
        /// </summary>
        /// <param name="arguments">Pipeline arguments</param>
        /// <returns>True if validation passes</returns>
        private async Task<bool> ValidateFileSystemAsync(PipelineArguments arguments)
        {
            await Task.Delay(10); // Simulate async work

            try
            {
                _logger.Information("Validating file system access");

                // Check input file exists and is readable
                if (!string.IsNullOrEmpty(arguments.SourceFilePath))
                {
                    if (!File.Exists(arguments.SourceFilePath))
                    {
                        _progressReporter.ReportStepError("File System Validation", 
                            $"Input file does not exist: {arguments.SourceFilePath}");
                        return false;
                    }

                    // Test read access
                    try
                    {
                        using var stream = File.OpenRead(arguments.SourceFilePath);
                        var fileSize = stream.Length;
                        _logger.Information("Input file validated: {FileName}, Size: {FileSize} bytes", 
                            arguments.SourceFilePath, fileSize);
                    }
                    catch (Exception ex)
                    {
                        _progressReporter.ReportStepError("File System Validation", 
                            $"Cannot read input file: {ex.Message}", ex);
                        return false;
                    }
                }

                // Validate output directory write access
                var outputDir = Path.GetDirectoryName(_configuration.GetPaperBillOutputPath(arguments.JobNumber));
                if (!string.IsNullOrEmpty(outputDir))
                {
                    try
                    {
                        var testFile = Path.Combine(outputDir, $"test_write_{Guid.NewGuid()}.tmp");
                        await File.WriteAllTextAsync(testFile, "test");
                        File.Delete(testFile);
                        _logger.Information("Output directory write access validated: {OutputDir}", outputDir);
                    }
                    catch (Exception ex)
                    {
                        _progressReporter.ReportStepError("File System Validation", 
                            $"Cannot write to output directory {outputDir}: {ex.Message}", ex);
                        return false;
                    }
                }

                _progressReporter.ReportStepCompleted("File System Validation", 
                    "All file system access checks passed");

                return true;
            }
            catch (Exception ex)
            {
                _progressReporter.ReportStepError("File System Validation", $"File system validation failed: {ex.Message}", ex);
                return false;
            }
        }
    }
}
