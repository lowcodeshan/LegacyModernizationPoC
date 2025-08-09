using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace LegacyModernization.Core.Configuration
{
    /// <summary>
    /// Configuration constants and settings for the legacy modernization pipeline
    /// Equivalent to environment variables and constants from the original script
    /// </summary>
    public class PipelineConfiguration
    {
        // Client and Service Configuration (from original script)
        public const string ClientDept = "250301";
        public const string ServiceType = "320";
        public const int ContainerKey = 1941;
        public const int OptionLength = 2000;
        public const int Work2Length = 4300;
        public const string ProjectType = "mblps";

        // File Extensions and Naming Conventions
        public const string SupplementalTableFile = "2503supptable.txt";
        public const string SupplementalExtension = ".se1";
        public const string ElectronicBillExtension = "e.txt";
        public const string PaperBillExtension = "p.asc";
        public const string DataFileExtension = ".dat";

        // Processing Parameters (from cnpsplit4.out)
        public const int SplitRecordLength = 2000;
        public const int SplitFieldLength = 1318;
        public const int SplitPosition = 1;
        public const string SplitType = "E";
        public const string OutputFormat = "ASCII";

        // Project Paths
        public string ProjectBase { get; set; } = string.Empty;
        public string InputPath { get; set; } = string.Empty;
        public string OutputPath { get; set; } = string.Empty;
        public string LogPath { get; set; } = string.Empty;
        public string TestDataPath { get; set; } = string.Empty;
        public string ExpectedOutputPath { get; set; } = string.Empty;

        /// <summary>
        /// Creates default configuration from appsettings.json fallback
        /// Used when environment variables are not set
        /// </summary>
        /// <param name="baseDirectory">Base directory for the project (optional, for solution root detection)</param>
        /// <returns>Configured pipeline settings</returns>
        public static PipelineConfiguration CreateDefault(string? baseDirectory = null)
        {
            // Try to find solution root if base directory not provided
            if (string.IsNullOrEmpty(baseDirectory))
            {
                baseDirectory = GetSolutionRoot() ?? Directory.GetCurrentDirectory();
            }

            // Build configuration from appsettings.json with environment variable overrides
            var configuration = new ConfigurationBuilder()
                .SetBasePath(GetConfigurationPath(baseDirectory))
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                .AddEnvironmentVariables()
                .Build();

            var configSection = configuration.GetSection("PipelineConfiguration");
            
            // Try to load from appsettings.json first
            var config = new PipelineConfiguration();
            configSection.Bind(config);

            // If appsettings.json doesn't have complete configuration, provide intelligent defaults
            if (string.IsNullOrEmpty(config.ProjectBase))
            {
                config.ProjectBase = baseDirectory;
                config.InputPath = Path.Combine(baseDirectory, "TestData");
                config.OutputPath = Path.Combine(baseDirectory, "Output");
                config.LogPath = Path.Combine(baseDirectory, "Logs");
                config.TestDataPath = Path.Combine(baseDirectory, "TestData");
                config.ExpectedOutputPath = Path.Combine(baseDirectory, "ExpectedOutput");
            }

            // Ensure directories exist
            Directory.CreateDirectory(config.InputPath);
            Directory.CreateDirectory(config.OutputPath);
            Directory.CreateDirectory(config.LogPath);
            Directory.CreateDirectory(config.TestDataPath);
            Directory.CreateDirectory(config.ExpectedOutputPath);

            return config;
        }

        /// <summary>
        /// Gets the configuration file path, prioritizing the Core project's appsettings.json
        /// </summary>
        /// <param name="baseDirectory">Base directory to search from</param>
        /// <returns>Path containing appsettings.json</returns>
        private static string GetConfigurationPath(string baseDirectory)
        {
            // Prioritize Core project's appsettings.json for centralized configuration
            var searchPaths = new[]
            {
                Path.Combine(baseDirectory, "LegacyModernization.Core"),  // Core project (highest priority)
                Directory.GetCurrentDirectory(),                         // Current directory
                baseDirectory                                           // Solution root
            };

            foreach (var path in searchPaths)
            {
                if (File.Exists(Path.Combine(path, "appsettings.json")))
                {
                    return path;
                }
            }

            // Default to base directory (appsettings.json will be optional)
            return baseDirectory;
        }

        /// <summary>
        /// Finds the solution root directory by looking for .sln files
        /// </summary>
        /// <returns>Solution root path or null if not found</returns>
        private static string? GetSolutionRoot()
        {
            var current = Directory.GetCurrentDirectory();
            while (current != null)
            {
                if (Directory.GetFiles(current, "*.sln").Length > 0)
                {
                    return current;
                }
                var parent = Directory.GetParent(current);
                current = parent?.FullName;
            }
            return null;
        }

        /// <summary>
        /// Creates configuration using environment variables (requires explicit configuration)
        /// This method requires all environment variables to be set explicitly and provides clear error messages if missing.
        /// Environment Variables (all required):
        /// - LEGACY_PROJECT_BASE: Base directory for the project
        /// - LEGACY_INPUT_PATH: Input files directory
        /// - LEGACY_OUTPUT_PATH: Output files directory  
        /// - LEGACY_LOG_PATH: Log files directory
        /// - LEGACY_TESTDATA_PATH: Test data files directory
        /// - LEGACY_EXPECTED_PATH: Expected output files directory for validation
        /// </summary>
        /// <returns>Environment-configured pipeline settings</returns>
        /// <exception cref="InvalidOperationException">Thrown when required environment variables are not set</exception>
        public static PipelineConfiguration CreateFromEnvironment()
        {
            var missingVars = new List<string>();

            // Check for required environment variables
            var projectBase = Environment.GetEnvironmentVariable("LEGACY_PROJECT_BASE");
            var inputPath = Environment.GetEnvironmentVariable("LEGACY_INPUT_PATH");
            var outputPath = Environment.GetEnvironmentVariable("LEGACY_OUTPUT_PATH");
            var logPath = Environment.GetEnvironmentVariable("LEGACY_LOG_PATH");
            var testDataPath = Environment.GetEnvironmentVariable("LEGACY_TESTDATA_PATH");
            var expectedPath = Environment.GetEnvironmentVariable("LEGACY_EXPECTED_PATH");

            if (string.IsNullOrEmpty(projectBase)) missingVars.Add("LEGACY_PROJECT_BASE");
            if (string.IsNullOrEmpty(inputPath)) missingVars.Add("LEGACY_INPUT_PATH");
            if (string.IsNullOrEmpty(outputPath)) missingVars.Add("LEGACY_OUTPUT_PATH");
            if (string.IsNullOrEmpty(logPath)) missingVars.Add("LEGACY_LOG_PATH");
            if (string.IsNullOrEmpty(testDataPath)) missingVars.Add("LEGACY_TESTDATA_PATH");
            if (string.IsNullOrEmpty(expectedPath)) missingVars.Add("LEGACY_EXPECTED_PATH");

            if (missingVars.Count > 0)
            {
                throw new InvalidOperationException(
                    $"Missing required environment variables for pipeline configuration:\n" +
                    $"  {string.Join("\n  ", missingVars)}\n\n" +
                    "Please set the following environment variables:\n" +
                    "  LEGACY_PROJECT_BASE=<base directory for the project>\n" +
                    "  LEGACY_INPUT_PATH=<input files directory>\n" +
                    "  LEGACY_OUTPUT_PATH=<output files directory>\n" +
                    "  LEGACY_LOG_PATH=<log files directory>\n" +
                    "  LEGACY_TESTDATA_PATH=<test data files directory>\n" +
                    "  LEGACY_EXPECTED_PATH=<expected output files directory>\n\n" +
                    "Example (Windows):\n" +
                    "  set LEGACY_PROJECT_BASE=C:\\Production\\LegacyModernization\n" +
                    "  set LEGACY_INPUT_PATH=\\\\FileServer\\MonthlyData\\Input\n" +
                    "  set LEGACY_OUTPUT_PATH=\\\\FileServer\\MonthlyData\\Output\n" +
                    "  set LEGACY_LOG_PATH=C:\\Logs\\Production\n" +
                    "  set LEGACY_TESTDATA_PATH=C:\\Production\\TestData\n" +
                    "  set LEGACY_EXPECTED_PATH=\\\\FileServer\\Validation\\Expected\n\n" +
                    "Example (Linux/Unix):\n" +
                    "  export LEGACY_PROJECT_BASE=/production/legacy-modernization\n" +
                    "  export LEGACY_INPUT_PATH=/data/monthly-input\n" +
                    "  export LEGACY_OUTPUT_PATH=/data/monthly-output\n" +
                    "  export LEGACY_LOG_PATH=/var/logs/legacy-modernization\n" +
                    "  export LEGACY_TESTDATA_PATH=/production/test-data\n" +
                    "  export LEGACY_EXPECTED_PATH=/data/validation/expected\n\n" +
                    "For development, you can use relative paths:\n" +
                    "  set LEGACY_PROJECT_BASE=C:\\Dev\\LegacyModernizationPoC\n" +
                    "  set LEGACY_INPUT_PATH=C:\\Dev\\LegacyModernizationPoC\\TestData\n" +
                    "  set LEGACY_OUTPUT_PATH=C:\\Dev\\LegacyModernizationPoC\\Output\n" +
                    "  set LEGACY_LOG_PATH=C:\\Dev\\LegacyModernizationPoC\\Logs\n" +
                    "  set LEGACY_TESTDATA_PATH=C:\\Dev\\LegacyModernizationPoC\\TestData\n" +
                    "  set LEGACY_EXPECTED_PATH=C:\\Dev\\LegacyModernizationPoC\\ExpectedOutput");
            }

            var config = new PipelineConfiguration
            {
                ProjectBase = projectBase!,
                InputPath = inputPath!,
                OutputPath = outputPath!,
                LogPath = logPath!,
                TestDataPath = testDataPath!,
                ExpectedOutputPath = expectedPath!
            };

            // Ensure directories exist
            Directory.CreateDirectory(config.InputPath);
            Directory.CreateDirectory(config.OutputPath);
            Directory.CreateDirectory(config.LogPath);
            Directory.CreateDirectory(config.TestDataPath);
            Directory.CreateDirectory(config.ExpectedOutputPath);

            return config;
        }

        /// <summary>
        /// Creates configuration with smart fallback: Environment Variables → appsettings.json → Error
        /// This provides a production-ready configuration hierarchy with clear fallback behavior
        /// </summary>
        /// <param name="baseDirectory">Base directory for the project (optional, for solution root detection)</param>
        /// <returns>Configured pipeline settings</returns>
        /// <exception cref="InvalidOperationException">Thrown when configuration cannot be resolved</exception>
        public static PipelineConfiguration CreateWithFallback(string? baseDirectory = null)
        {
            // Try environment variables first
            var missingEnvVars = new List<string>();
            var projectBase = Environment.GetEnvironmentVariable("LEGACY_PROJECT_BASE");
            var inputPath = Environment.GetEnvironmentVariable("LEGACY_INPUT_PATH");
            var outputPath = Environment.GetEnvironmentVariable("LEGACY_OUTPUT_PATH");
            var logPath = Environment.GetEnvironmentVariable("LEGACY_LOG_PATH");
            var testDataPath = Environment.GetEnvironmentVariable("LEGACY_TESTDATA_PATH");
            var expectedPath = Environment.GetEnvironmentVariable("LEGACY_EXPECTED_PATH");

            if (string.IsNullOrEmpty(projectBase)) missingEnvVars.Add("LEGACY_PROJECT_BASE");
            if (string.IsNullOrEmpty(inputPath)) missingEnvVars.Add("LEGACY_INPUT_PATH");
            if (string.IsNullOrEmpty(outputPath)) missingEnvVars.Add("LEGACY_OUTPUT_PATH");
            if (string.IsNullOrEmpty(logPath)) missingEnvVars.Add("LEGACY_LOG_PATH");
            if (string.IsNullOrEmpty(testDataPath)) missingEnvVars.Add("LEGACY_TESTDATA_PATH");
            if (string.IsNullOrEmpty(expectedPath)) missingEnvVars.Add("LEGACY_EXPECTED_PATH");

            // If all environment variables are set, use them
            if (missingEnvVars.Count == 0)
            {
                return CreateFromEnvironment();
            }

            // Fall back to appsettings.json
            if (string.IsNullOrEmpty(baseDirectory))
            {
                baseDirectory = GetSolutionRoot() ?? Directory.GetCurrentDirectory();
            }

            var configPath = GetConfigurationPath(baseDirectory);
            var appSettingsPath = Path.Combine(configPath, "appsettings.json");

            if (File.Exists(appSettingsPath))
            {
                Console.WriteLine($"Environment variables not set, using appsettings.json configuration: {appSettingsPath}");
                
                var configuration = new ConfigurationBuilder()
                    .SetBasePath(configPath)
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                    .AddEnvironmentVariables()
                    .Build();

                var configSection = configuration.GetSection("PipelineConfiguration");
                var config = new PipelineConfiguration();
                configSection.Bind(config);

                // Validate that appsettings.json has complete configuration
                if (string.IsNullOrEmpty(config.ProjectBase) ||
                    string.IsNullOrEmpty(config.InputPath) ||
                    string.IsNullOrEmpty(config.OutputPath) ||
                    string.IsNullOrEmpty(config.LogPath) ||
                    string.IsNullOrEmpty(config.TestDataPath) ||
                    string.IsNullOrEmpty(config.ExpectedOutputPath))
                {
                    throw new InvalidOperationException(
                        $"Incomplete configuration in appsettings.json: {appSettingsPath}\n" +
                        "Please ensure all required fields are set in the PipelineConfiguration section:\n" +
                        "  - ProjectBase\n" +
                        "  - InputPath\n" +
                        "  - OutputPath\n" +
                        "  - LogPath\n" +
                        "  - TestDataPath\n" +
                        "  - ExpectedOutputPath");
                }

                // Ensure directories exist
                Directory.CreateDirectory(config.InputPath);
                Directory.CreateDirectory(config.OutputPath);
                Directory.CreateDirectory(config.LogPath);
                Directory.CreateDirectory(config.TestDataPath);
                Directory.CreateDirectory(config.ExpectedOutputPath);

                return config;
            }

            // No valid configuration source found
            throw new InvalidOperationException(
                "No valid configuration found. Please either:\n\n" +
                "1. Set environment variables:\n" +
                $"   Missing: {string.Join(", ", missingEnvVars)}\n\n" +
                "2. Create appsettings.json with PipelineConfiguration section:\n" +
                $"   Expected location: {appSettingsPath}\n\n" +
                "3. Use CreateDefault() method for development with auto-generated paths\n\n" +
                GetEnvironmentVariableExamples());
        }

        /// <summary>
        /// <summary>
        /// Gets detailed environment variable setup examples
        /// </summary>
        /// <returns>Example configuration text</returns>
        private static string GetEnvironmentVariableExamples()
        {
            return "Environment Variable Examples:\n" +
                   "Windows:\n" +
                   "  set LEGACY_PROJECT_BASE=C:\\Production\\LegacyModernization\n" +
                   "  set LEGACY_INPUT_PATH=\\\\FileServer\\MonthlyData\\Input\n" +
                   "  set LEGACY_OUTPUT_PATH=\\\\FileServer\\MonthlyData\\Output\n" +
                   "  set LEGACY_LOG_PATH=C:\\Logs\\Production\n" +
                   "  set LEGACY_TESTDATA_PATH=C:\\Production\\TestData\n" +
                   "  set LEGACY_EXPECTED_PATH=\\\\FileServer\\Validation\\Expected\n\n" +
                   "Linux/Unix:\n" +
                   "  export LEGACY_PROJECT_BASE=/production/legacy-modernization\n" +
                   "  export LEGACY_INPUT_PATH=/data/monthly-input\n" +
                   "  export LEGACY_OUTPUT_PATH=/data/monthly-output\n" +
                   "  export LEGACY_LOG_PATH=/var/logs/legacy-modernization\n" +
                   "  export LEGACY_TESTDATA_PATH=/production/test-data\n" +
                   "  export LEGACY_EXPECTED_PATH=/data/validation/expected";
        }

        /// <summary>
        /// Creates production configuration with preference for environment variables but appsettings.json fallback
        /// For strict environment-only configuration, use CreateFromEnvironment() directly
        /// </summary>
        /// <returns>Production-ready pipeline configuration</returns>
        /// <exception cref="InvalidOperationException">Thrown when no valid configuration source is found</exception>
        public static PipelineConfiguration CreateForProduction()
        {
            return CreateWithFallback();
        }

        /// <summary>
        /// Gets input file path for a given job number
        /// </summary>
        /// <param name="jobNumber">Job number</param>
        /// <returns>Full path to input data file</returns>
        public string GetInputFilePath(string jobNumber)
        {
            return Path.Combine(InputPath, $"{jobNumber}{DataFileExtension}");
        }

        /// <summary>
        /// Gets output file path for paper bills
        /// </summary>
        /// <param name="jobNumber">Job number</param>
        /// <returns>Full path to paper bill output file</returns>
        public string GetPaperBillOutputPath(string jobNumber)
        {
            return Path.Combine(OutputPath, $"{jobNumber}{PaperBillExtension}");
        }

        /// <summary>
        /// Gets output file path for electronic bills
        /// </summary>
        /// <param name="jobNumber">Job number</param>
        /// <returns>Full path to electronic bill output file</returns>
        public string GetElectronicBillOutputPath(string jobNumber)
        {
            return Path.Combine(OutputPath, $"{jobNumber}{ElectronicBillExtension}");
        }

        /// <summary>
        /// Gets supplemental table file path
        /// </summary>
        /// <param name="jobNumber">Job number</param>
        /// <returns>Full path to supplemental table file</returns>
        public string GetSupplementalTablePath(string jobNumber)
        {
            return Path.Combine(OutputPath, $"{jobNumber}{SupplementalExtension}");
        }

        /// <summary>
        /// Validates configuration settings
        /// </summary>
        /// <returns>True if configuration is valid</returns>
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(ProjectBase) &&
                   Directory.Exists(ProjectBase) &&
                   !string.IsNullOrEmpty(InputPath) &&
                   !string.IsNullOrEmpty(OutputPath) &&
                   !string.IsNullOrEmpty(LogPath);
        }

        /// <summary>
        /// Gets configuration summary for logging
        /// </summary>
        /// <returns>Configuration summary string</returns>
        public override string ToString()
        {
            return $"Pipeline Configuration:\n" +
                   $"  Project Base: {ProjectBase}\n" +
                   $"  Input Path: {InputPath}\n" +
                   $"  Output Path: {OutputPath}\n" +
                   $"  Log Path: {LogPath}\n" +
                   $"  Client Dept: {ClientDept}\n" +
                   $"  Service Type: {ServiceType}\n" +
                   $"  Container Key: {ContainerKey}\n" +
                   $"  Project Type: {ProjectType}";
        }
    }

    /// <summary>
    /// Command line arguments configuration
    /// </summary>
    public class PipelineArguments
    {
        public string JobNumber { get; set; } = string.Empty;
        public string SourceFilePath { get; set; } = string.Empty;
        public bool Verbose { get; set; } = false;
        public bool DryRun { get; set; } = false;
        public string LogLevel { get; set; } = "Information";

        /// <summary>
        /// Validates command line arguments using enhanced ArgumentValidator
        /// </summary>
        /// <returns>True if arguments are valid</returns>
        public bool IsValid()
        {
            var validationResult = ArgumentValidator.ValidateArguments(this);
            return validationResult.IsValid;
        }

        /// <summary>
        /// Gets usage information for command line help
        /// </summary>
        /// <returns>Usage string</returns>
        public static string GetUsage()
        {
            return "Usage: LegacyModernization.Pipeline <job-number> [options]\n" +
                   "\n" +
                   "Arguments:\n" +
                   "  job-number               Job number for processing (e.g., 69172)\n" +
                   "\n" +
                   "Options:\n" +
                   "  -s, --source-file <path> Path to source data file (optional)\n" +
                   "  -v, --verbose            Enable verbose logging\n" +
                   "  -d, --dry-run           Validate inputs without processing\n" +
                   "  -l, --log-level <level> Set log level (Debug, Information, Warning, Error)\n" +
                   "  -h, --help              Show help information\n" +
                   "\n" +
                   "Examples:\n" +
                   "  LegacyModernization.Pipeline 69172\n" +
                   "  LegacyModernization.Pipeline 69172 --source-file \"C:\\Data\\69172.dat\" --verbose\n" +
                   "  LegacyModernization.Pipeline 69172 --dry-run --log-level Debug";
        }
    }
}
