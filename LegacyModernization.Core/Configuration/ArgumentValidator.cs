using System;
using System.IO;
using System.Text.RegularExpressions;

namespace LegacyModernization.Core.Configuration
{
    /// <summary>
    /// Enhanced validation for pipeline arguments matching legacy script requirements
    /// Implements validation equivalent to lines 1-30 of mbcntr2503.script
    /// </summary>
    public static class ArgumentValidator
    {
        /// <summary>
        /// Job number regex pattern - numeric string typically 5 digits
        /// </summary>
        private static readonly Regex JobNumberPattern = new Regex(@"^\d{4,6}$", RegexOptions.Compiled);

        /// <summary>
        /// Validates job number format and range equivalent to $1 validation in legacy script
        /// </summary>
        /// <param name="jobNumber">Job number string</param>
        /// <returns>Validation result with details</returns>
        public static ValidationResult ValidateJobNumber(string jobNumber)
        {
            if (string.IsNullOrWhiteSpace(jobNumber))
            {
                return ValidationResult.Failure("Job number is required");
            }

            if (!JobNumberPattern.IsMatch(jobNumber))
            {
                return ValidationResult.Failure("Job number must be 4-6 digits (e.g., 69172)");
            }

            if (!int.TryParse(jobNumber, out int jobNum))
            {
                return ValidationResult.Failure("Job number must be numeric");
            }

            if (jobNum <= 0)
            {
                return ValidationResult.Failure("Job number must be positive");
            }

            // Additional range validation based on typical job number patterns
            if (jobNum < 1000 || jobNum > 999999)
            {
                return ValidationResult.Failure("Job number must be between 1000 and 999999");
            }

            return ValidationResult.Success();
        }

        /// <summary>
        /// Validates source file parameter and existence check equivalent to $2 validation
        /// </summary>
        /// <param name="sourceFilePath">Source file path</param>
        /// <param name="isRequired">Whether source file is required</param>
        /// <returns>Validation result with details</returns>
        public static ValidationResult ValidateSourceFile(string sourceFilePath, bool isRequired = false)
        {
            if (string.IsNullOrWhiteSpace(sourceFilePath))
            {
                return isRequired 
                    ? ValidationResult.Failure("Source file parameter is required")
                    : ValidationResult.Success();
            }

            if (!File.Exists(sourceFilePath))
            {
                return ValidationResult.Failure($"Source file does not exist: {sourceFilePath}");
            }

            var fileInfo = new FileInfo(sourceFilePath);
            if (fileInfo.Length == 0)
            {
                return ValidationResult.Failure($"Source file is empty: {sourceFilePath}");
            }

            // Validate file extension if it's a data file
            if (sourceFilePath.EndsWith(".dat", StringComparison.OrdinalIgnoreCase))
            {
                // Additional validation for .dat files could be added here
                // For now, just check it's readable
                try
                {
                    using var stream = File.OpenRead(sourceFilePath);
                    // File is readable
                }
                catch (Exception ex)
                {
                    return ValidationResult.Failure($"Cannot read source file: {ex.Message}");
                }
            }

            return ValidationResult.Success();
        }

        /// <summary>
        /// Validates environment and configuration setup
        /// </summary>
        /// <param name="config">Pipeline configuration</param>
        /// <returns>Validation result with details</returns>
        public static ValidationResult ValidateEnvironment(PipelineConfiguration config)
        {
            if (config == null)
            {
                return ValidationResult.Failure("Configuration is null");
            }

            if (!config.IsValid())
            {
                return ValidationResult.Failure("Configuration validation failed");
            }

            // Validate required directories exist or can be created
            try
            {
                Directory.CreateDirectory(config.OutputPath);
                Directory.CreateDirectory(config.LogPath);
            }
            catch (Exception ex)
            {
                return ValidationResult.Failure($"Cannot create required directories: {ex.Message}");
            }

            return ValidationResult.Success();
        }

        /// <summary>
        /// Comprehensive argument validation matching legacy script usage requirements
        /// </summary>
        /// <param name="arguments">Pipeline arguments</param>
        /// <returns>Validation result with details</returns>
        public static ValidationResult ValidateArguments(PipelineArguments arguments)
        {
            if (arguments == null)
            {
                return ValidationResult.Failure("Arguments are null");
            }

            // Validate job number (equivalent to $1 validation)
            var jobValidation = ValidateJobNumber(arguments.JobNumber);
            if (!jobValidation.IsValid)
            {
                return jobValidation;
            }

            // Validate source file if provided (equivalent to $2 validation)
            var sourceValidation = ValidateSourceFile(arguments.SourceFilePath, false);
            if (!sourceValidation.IsValid)
            {
                return sourceValidation;
            }

            // Validate log level
            if (!IsValidLogLevel(arguments.LogLevel))
            {
                return ValidationResult.Failure($"Invalid log level: {arguments.LogLevel}. Valid levels: Debug, Information, Warning, Error");
            }

            return ValidationResult.Success();
        }

        /// <summary>
        /// Validates log level string
        /// </summary>
        /// <param name="logLevel">Log level to validate</param>
        /// <returns>True if valid log level</returns>
        private static bool IsValidLogLevel(string logLevel)
        {
            if (string.IsNullOrWhiteSpace(logLevel))
                return false;

            var validLevels = new[] { "Debug", "Information", "Warning", "Error", "Fatal" };
            return Array.Exists(validLevels, level => 
                string.Equals(level, logLevel, StringComparison.OrdinalIgnoreCase));
        }
    }

    /// <summary>
    /// Validation result with success status and error message
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; private set; }
        public string ErrorMessage { get; private set; } = string.Empty;

        private ValidationResult(bool isValid, string errorMessage = "")
        {
            IsValid = isValid;
            ErrorMessage = errorMessage;
        }

        public static ValidationResult Success() => new ValidationResult(true);
        public static ValidationResult Failure(string errorMessage) => new ValidationResult(false, errorMessage);

        public override string ToString() => IsValid ? "Valid" : $"Invalid: {ErrorMessage}";
    }
}
