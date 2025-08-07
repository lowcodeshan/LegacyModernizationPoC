using Serilog;
using Serilog.Events;
using System;
using System.IO;

namespace LegacyModernization.Core.Logging
{
    /// <summary>
    /// Centralized logging configuration for the legacy modernization pipeline
    /// </summary>
    public static class LoggerConfiguration
    {
        /// <summary>
        /// Configures Serilog logger for pipeline execution tracking
        /// </summary>
        /// <param name="logDirectory">Directory for log files</param>
        /// <param name="jobNumber">Job number for log file naming</param>
        /// <returns>Configured logger instance</returns>
        public static ILogger CreateLogger(string logDirectory, string jobNumber = "default")
        {
            // Ensure log directory exists
            Directory.CreateDirectory(logDirectory);

            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var logFileName = $"pipeline_{jobNumber}_{timestamp}.log";
            var logFilePath = Path.Combine(logDirectory, logFileName);

            return new Serilog.LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .MinimumLevel.Override("System", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("JobNumber", jobNumber)
                .Enrich.WithProperty("ProcessId", Environment.ProcessId)
                .Enrich.WithProperty("MachineName", Environment.MachineName)
                .WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties}{NewLine}{Exception}")
                .WriteTo.File(
                    logFilePath,
                    outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {Message:lj} {Properties}{NewLine}{Exception}",
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30)
                .CreateLogger();
        }

        /// <summary>
        /// Creates a logger specifically for pipeline step execution
        /// </summary>
        /// <param name="logDirectory">Directory for log files</param>
        /// <param name="jobNumber">Job number for context</param>
        /// <param name="stepName">Pipeline step name</param>
        /// <returns>Configured logger with step context</returns>
        public static ILogger CreateStepLogger(string logDirectory, string jobNumber, string stepName)
        {
            var baseLogger = CreateLogger(logDirectory, jobNumber);
            return baseLogger.ForContext("Step", stepName);
        }
    }

    /// <summary>
    /// Pipeline execution context for structured logging
    /// </summary>
    public class PipelineExecutionContext
    {
        public string JobNumber { get; set; } = string.Empty;
        public string InputFilePath { get; set; } = string.Empty;
        public string OutputDirectory { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public string CurrentStep { get; set; } = string.Empty;
        public long ProcessedRecords { get; set; }
        public long TotalRecords { get; set; }

        public double ProgressPercentage => TotalRecords > 0 ? (double)ProcessedRecords / TotalRecords * 100 : 0;
    }

    /// <summary>
    /// Extensions for structured logging of pipeline events
    /// </summary>
    public static class PipelineLoggingExtensions
    {
        /// <summary>
        /// Logs pipeline start event
        /// </summary>
        public static void LogPipelineStart(this ILogger logger, PipelineExecutionContext context)
        {
            logger.Information("Pipeline execution started for job {JobNumber} with input file {InputFilePath}",
                context.JobNumber, context.InputFilePath);
        }

        /// <summary>
        /// Logs pipeline step start
        /// </summary>
        public static void LogStepStart(this ILogger logger, string stepName, PipelineExecutionContext context)
        {
            logger.Information("Starting step {StepName} for job {JobNumber}",
                stepName, context.JobNumber);
        }

        /// <summary>
        /// Logs pipeline step completion
        /// </summary>
        public static void LogStepComplete(this ILogger logger, string stepName, PipelineExecutionContext context, TimeSpan duration)
        {
            logger.Information("Completed step {StepName} for job {JobNumber} in {Duration}ms. Processed {ProcessedRecords} records",
                stepName, context.JobNumber, duration.TotalMilliseconds, context.ProcessedRecords);
        }

        /// <summary>
        /// Logs pipeline step error
        /// </summary>
        public static void LogStepError(this ILogger logger, string stepName, PipelineExecutionContext context, Exception exception)
        {
            logger.Error(exception, "Step {StepName} failed for job {JobNumber} after processing {ProcessedRecords} records",
                stepName, context.JobNumber, context.ProcessedRecords);
        }

        /// <summary>
        /// Logs progress update
        /// </summary>
        public static void LogProgress(this ILogger logger, PipelineExecutionContext context)
        {
            logger.Debug("Progress update for job {JobNumber}: {ProcessedRecords}/{TotalRecords} records ({ProgressPercentage:F1}%)",
                context.JobNumber, context.ProcessedRecords, context.TotalRecords, context.ProgressPercentage);
        }

        /// <summary>
        /// Logs file operation
        /// </summary>
        public static void LogFileOperation(this ILogger logger, string operation, string filePath, long? fileSize = null)
        {
            if (fileSize.HasValue)
            {
                logger.Debug("File operation: {Operation} - {FilePath} ({FileSize} bytes)",
                    operation, filePath, fileSize.Value);
            }
            else
            {
                logger.Debug("File operation: {Operation} - {FilePath}",
                    operation, filePath);
            }
        }

        /// <summary>
        /// Logs validation result
        /// </summary>
        public static void LogValidationResult(this ILogger logger, string validationType, bool isValid, string details = "")
        {
            if (isValid)
            {
                logger.Information("Validation passed: {ValidationType} {Details}",
                    validationType, details);
            }
            else
            {
                logger.Warning("Validation failed: {ValidationType} {Details}",
                    validationType, details);
            }
        }
    }
}
