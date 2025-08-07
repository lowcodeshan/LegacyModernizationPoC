using System;
using System.IO;

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
        /// Creates default configuration for development environment
        /// </summary>
        /// <param name="baseDirectory">Base directory for the project</param>
        /// <returns>Configured pipeline settings</returns>
        public static PipelineConfiguration CreateDefault(string baseDirectory)
        {
            var config = new PipelineConfiguration
            {
                ProjectBase = baseDirectory,
                InputPath = Path.Combine(baseDirectory, "TestData"),
                OutputPath = Path.Combine(baseDirectory, "Output"),
                LogPath = Path.Combine(baseDirectory, "Logs"),
                TestDataPath = Path.Combine(baseDirectory, "TestData"),
                ExpectedOutputPath = Path.Combine(baseDirectory, "ExpectedOutput")
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
        /// Validates command line arguments
        /// </summary>
        /// <returns>True if arguments are valid</returns>
        public bool IsValid()
        {
            // Job number should be numeric and within expected range
            if (!int.TryParse(JobNumber, out int jobNum) || jobNum <= 0)
                return false;

            // Source file should exist if specified
            if (!string.IsNullOrEmpty(SourceFilePath) && !File.Exists(SourceFilePath))
                return false;

            return true;
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
