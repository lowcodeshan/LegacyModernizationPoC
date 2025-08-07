using LegacyModernization.Core.Configuration;
using LegacyModernization.Core.Logging;
using Serilog;
using System;
using System.IO;
using System.Threading.Tasks;

namespace LegacyModernization.Pipeline
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            // Initialize configuration
            var projectBase = Path.GetDirectoryName(AppContext.BaseDirectory) 
                ?? throw new InvalidOperationException("Could not determine project base directory");
            
            // Navigate up to solution root (typically 4 levels: bin/Debug/net8.0/LegacyModernization.Pipeline -> project root)
            var solutionRoot = GetSolutionRoot(projectBase);
            var config = PipelineConfiguration.CreateDefault(solutionRoot);

            // Create logger
            var logger = Core.Logging.LoggerConfiguration.CreateLogger(config.LogPath, "initialization");
            
            try
            {
                // Display banner
                DisplayBanner(logger);
                
                // Parse arguments
                var arguments = ParseArguments(args);
                if (arguments == null)
                {
                    DisplayUsage();
                    return 1;
                }

                // Validate arguments
                if (!arguments.IsValid())
                {
                    logger.Error("Invalid arguments provided");
                    DisplayUsage();
                    return 1;
                }

                logger.Information("Starting Legacy Modernization Pipeline for job {JobNumber}", arguments.JobNumber);
                logger.Information("Configuration: {Configuration}", config.ToString());

                // TODO: Initialize and run pipeline steps
                // This will be implemented in subsequent tasks

                logger.Information("Pipeline initialization completed successfully");
                return 0;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Fatal error during pipeline initialization");
                return 1;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        private static void DisplayBanner(ILogger logger)
        {
            var banner = @"
==============================================================================
                    Legacy Modernization Pipeline
                         Monthly Bill Process 2503
                               PoC Version 1.0
==============================================================================";
            
            Console.WriteLine(banner);
            logger.Information("Legacy Modernization Pipeline started at {Timestamp}", DateTime.Now);
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
