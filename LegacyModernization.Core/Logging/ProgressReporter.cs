using Serilog;
using System;

namespace LegacyModernization.Core.Logging
{
    /// <summary>
    /// Progress reporting mechanism for user feedback during pipeline execution
    /// Implements logging and banner functionality equivalent to mbcntr2503.script
    /// </summary>
    public class ProgressReporter
    {
        private readonly ILogger _logger;
        private readonly bool _verbose;
        private int _currentStep = 0;
        private int _totalSteps = 0;

        public ProgressReporter(ILogger logger, bool verbose = false)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _verbose = verbose;
        }

        /// <summary>
        /// Displays startup banner matching "Monthly Bill... 2503" output from legacy script
        /// </summary>
        /// <param name="version">Version string for the banner</param>
        public void DisplayStartupBanner(string version = "PoC Version 1.0")
        {
            var timestamp = DateTime.Now;
            
            var banner = @"
==============================================================================
                    Legacy Modernization Pipeline
                         Monthly Bill Process 2503
                               " + version + @"
==============================================================================
                        Started: " + timestamp.ToString("yyyy-MM-dd HH:mm:ss") + @"
==============================================================================";
            
            Console.WriteLine(banner);
            _logger.Information("Legacy Modernization Pipeline started at {Timestamp}", timestamp);
            
            if (_verbose)
            {
                Console.WriteLine($"Process ID: {Environment.ProcessId}");
                Console.WriteLine($"Working Directory: {Environment.CurrentDirectory}");
                Console.WriteLine($"User: {Environment.UserName}");
                Console.WriteLine($"Machine: {Environment.MachineName}");
                Console.WriteLine();
            }
        }

        /// <summary>
        /// Initialize progress tracking for pipeline steps
        /// </summary>
        /// <param name="totalSteps">Total number of steps in the pipeline</param>
        public void InitializeProgress(int totalSteps)
        {
            _totalSteps = totalSteps;
            _currentStep = 0;
            
            Console.WriteLine($"Pipeline initialized with {totalSteps} steps");
            _logger.Information("Pipeline progress tracking initialized with {TotalSteps} steps", totalSteps);
        }

        /// <summary>
        /// Report progress for a specific step
        /// </summary>
        /// <param name="stepName">Name of the current step</param>
        /// <param name="status">Status message</param>
        /// <param name="incrementStep">Whether to increment the step counter</param>
        public void ReportStep(string stepName, string status, bool incrementStep = true)
        {
            if (incrementStep)
            {
                _currentStep++;
            }

            var progressPercent = _totalSteps > 0 ? (double)_currentStep / _totalSteps * 100 : 0;
            var progressBar = CreateProgressBar(progressPercent);
            
            Console.WriteLine($"[Step {_currentStep}/{_totalSteps}] {stepName}: {status}");
            
            if (_verbose)
            {
                Console.WriteLine($"Progress: {progressBar} {progressPercent:F1}%");
            }
            
            _logger.Information("Step {StepNumber}/{TotalSteps}: {StepName} - {Status}", 
                _currentStep, _totalSteps, stepName, status);
        }

        /// <summary>
        /// Report successful completion of a step
        /// </summary>
        /// <param name="stepName">Name of the completed step</param>
        /// <param name="additionalInfo">Additional information about the completion</param>
        public void ReportStepCompleted(string stepName, string additionalInfo = "")
        {
            var message = $"✓ {stepName} completed successfully";
            if (!string.IsNullOrEmpty(additionalInfo))
            {
                message += $" - {additionalInfo}";
            }
            
            Console.WriteLine(message);
            _logger.Information("Step completed: {StepName} {AdditionalInfo}", stepName, additionalInfo);
        }

        /// <summary>
        /// Report an error in a step
        /// </summary>
        /// <param name="stepName">Name of the step that failed</param>
        /// <param name="error">Error message</param>
        /// <param name="exception">Optional exception details</param>
        public void ReportStepError(string stepName, string error, Exception? exception = null)
        {
            Console.WriteLine($"✗ {stepName} failed: {error}");
            
            if (exception != null)
            {
                _logger.Error(exception, "Step failed: {StepName} - {Error}", stepName, error);
                
                if (_verbose)
                {
                    Console.WriteLine($"Error details: {exception.Message}");
                    if (exception.InnerException != null)
                    {
                        Console.WriteLine($"Inner exception: {exception.InnerException.Message}");
                    }
                }
            }
            else
            {
                _logger.Error("Step failed: {StepName} - {Error}", stepName, error);
            }
        }

        /// <summary>
        /// Report warning for a step
        /// </summary>
        /// <param name="stepName">Name of the step</param>
        /// <param name="warning">Warning message</param>
        public void ReportStepWarning(string stepName, string warning)
        {
            Console.WriteLine($"⚠ {stepName}: {warning}");
            _logger.Warning("Step warning: {StepName} - {Warning}", stepName, warning);
        }

        /// <summary>
        /// Report final pipeline completion
        /// </summary>
        /// <param name="success">Whether the pipeline completed successfully</param>
        /// <param name="duration">Total execution duration</param>
        public void ReportCompletion(bool success, TimeSpan duration)
        {
            var timestamp = DateTime.Now;
            
            if (success)
            {
                Console.WriteLine();
                Console.WriteLine("==============================================================================");
                Console.WriteLine("                    PIPELINE COMPLETED SUCCESSFULLY");
                Console.WriteLine($"                        Duration: {duration:hh\\:mm\\:ss}");
                Console.WriteLine($"                        Ended: {timestamp:yyyy-MM-dd HH:mm:ss}");
                Console.WriteLine("==============================================================================");
                
                _logger.Information("Pipeline completed successfully at {Timestamp} after {Duration}", 
                    timestamp, duration);
            }
            else
            {
                Console.WriteLine();
                Console.WriteLine("==============================================================================");
                Console.WriteLine("                       PIPELINE FAILED");
                Console.WriteLine($"                        Duration: {duration:hh\\:mm\\:ss}");
                Console.WriteLine($"                        Failed at: {timestamp:yyyy-MM-dd HH:mm:ss}");
                Console.WriteLine("==============================================================================");
                
                _logger.Error("Pipeline failed at {Timestamp} after {Duration}", timestamp, duration);
            }
        }

        /// <summary>
        /// Create a simple text progress bar
        /// </summary>
        /// <param name="percent">Completion percentage (0-100)</param>
        /// <returns>Progress bar string</returns>
        private static string CreateProgressBar(double percent)
        {
            const int barLength = 20;
            var filledLength = (int)(percent / 100 * barLength);
            var bar = new string('█', filledLength) + new string('░', barLength - filledLength);
            return $"[{bar}]";
        }

        /// <summary>
        /// Log configuration information for debugging
        /// </summary>
        /// <param name="config">Pipeline configuration</param>
        public void LogConfiguration(Configuration.PipelineConfiguration config)
        {
            _logger.Information("Pipeline Configuration Details:");
            _logger.Information("  Project Base: {ProjectBase}", config.ProjectBase);
            _logger.Information("  Input Path: {InputPath}", config.InputPath);
            _logger.Information("  Output Path: {OutputPath}", config.OutputPath);
            _logger.Information("  Log Path: {LogPath}", config.LogPath);
            _logger.Information("  Client Dept: {ClientDept}", Configuration.PipelineConfiguration.ClientDept);
            _logger.Information("  Service Type: {ServiceType}", Configuration.PipelineConfiguration.ServiceType);
            _logger.Information("  Container Key: {ContainerKey}", Configuration.PipelineConfiguration.ContainerKey);
            _logger.Information("  Project Type: {ProjectType}", Configuration.PipelineConfiguration.ProjectType);
            
            if (_verbose)
            {
                Console.WriteLine("Configuration loaded:");
                Console.WriteLine($"  Project Base: {config.ProjectBase}");
                Console.WriteLine($"  Input Path: {config.InputPath}");
                Console.WriteLine($"  Output Path: {config.OutputPath}");
                Console.WriteLine($"  Log Path: {config.LogPath}");
                Console.WriteLine();
            }
        }
    }
}
