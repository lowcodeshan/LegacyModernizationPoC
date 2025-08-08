using LegacyModernization.Core.Configuration;
using LegacyModernization.Core.DataAccess;
using LegacyModernization.Core.Logging;
using LegacyModernization.Core.Models;
using Serilog;
using System;
using System.IO;
using System.Threading.Tasks;

namespace LegacyModernization.Core.Pipeline
{
    /// <summary>
    /// Supplemental File Processing Component
    /// Implements the supplemental file copying logic equivalent to lines 45-47 of mbcntr2503.script
    /// Handles: cp /users/programs/2503supptable.txt /users/public/$job.se1
    /// </summary>
    public class SupplementalFileProcessingComponent
    {
        private readonly ILogger _logger;
        private readonly ProgressReporter _progressReporter;
        private readonly PipelineConfiguration _configuration;
        private readonly SupplementalTableParser _parser;

        public SupplementalFileProcessingComponent(
            ILogger logger,
            ProgressReporter progressReporter,
            PipelineConfiguration configuration)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _progressReporter = progressReporter ?? throw new ArgumentNullException(nameof(progressReporter));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _parser = new SupplementalTableParser();
        }

        /// <summary>
        /// Execute supplemental file processing
        /// </summary>
        /// <param name="arguments">Pipeline arguments</param>
        /// <returns>Task representing the async operation with success status</returns>
        public async Task<bool> ExecuteAsync(PipelineArguments arguments)
        {
            try
            {
                _progressReporter.ReportStep("Supplemental File Processing", "Starting supplemental file processing", false);

                // Step 1: Validate and locate source supplemental table file
                var sourceSupplementalFile = await ValidateSourceSupplementalFileAsync();
                if (string.IsNullOrEmpty(sourceSupplementalFile))
                {
                    return false;
                }

                // Step 2: Parse and validate supplemental table structure
                var supplementalData = await ParseSupplementalTableAsync(sourceSupplementalFile);
                if (supplementalData == null)
                {
                    return false;
                }

                // Step 3: Copy supplemental file to job-specific location (equivalent to cp command)
                var targetSupplementalFile = await CopySupplementalFileAsync(arguments.JobNumber, sourceSupplementalFile);
                if (string.IsNullOrEmpty(targetSupplementalFile))
                {
                    return false;
                }

                // Step 4: Integrate with client configuration system
                if (!await IntegrateWithClientConfigurationAsync(arguments, supplementalData))
                {
                    return false;
                }

                _progressReporter.ReportStep("Supplemental File Processing",
                    $"Job {arguments.JobNumber} supplemental file processed successfully", true);

                return true;
            }
            catch (Exception ex)
            {
                _progressReporter.ReportStepError("Supplemental File Processing", ex.Message, ex);
                return false;
            }
        }

        /// <summary>
        /// Validates and locates the source supplemental table file
        /// </summary>
        /// <returns>Path to valid supplemental table file or null if not found</returns>
        private async Task<string?> ValidateSourceSupplementalFileAsync()
        {
            await Task.Delay(10); // Simulate async work

            _logger.Information("Validating source supplemental table file");

            // Look for supplemental table file in several possible locations
            var possiblePaths = new[]
            {
                Path.Combine(_configuration.TestDataPath, PipelineConfiguration.SupplementalTableFile),
                Path.Combine(_configuration.ProjectBase, "TestData", PipelineConfiguration.SupplementalTableFile),
                Path.Combine(_configuration.InputPath, PipelineConfiguration.SupplementalTableFile),
                Path.Combine(_configuration.ProjectBase, PipelineConfiguration.SupplementalTableFile)
            };

            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    _logger.Information("Found supplemental table file: {FilePath}", path);
                    
                    // Validate file is readable and not empty
                    try
                    {
                        var fileInfo = new FileInfo(path);
                        if (fileInfo.Length == 0)
                        {
                            _progressReporter.ReportStepWarning("Source File Validation", 
                                $"Supplemental table file is empty: {path}");
                            continue;
                        }

                        // Test read access
                        using var stream = File.OpenRead(path);
                        _logger.Information("Supplemental table file validated: {FilePath}, Size: {FileSize} bytes", 
                            path, fileInfo.Length);
                        
                        _progressReporter.ReportStepCompleted("Source File Validation", 
                            $"Found and validated: {Path.GetFileName(path)}");
                        return path;
                    }
                    catch (Exception ex)
                    {
                        _progressReporter.ReportStepWarning("Source File Validation", 
                            $"Cannot read supplemental table file {path}: {ex.Message}");
                        continue;
                    }
                }
            }

            _progressReporter.ReportStepError("Source File Validation", 
                $"Supplemental table file '{PipelineConfiguration.SupplementalTableFile}' not found in any expected location");
            _logger.Error("Supplemental table file not found. Searched paths: {SearchPaths}", string.Join(", ", possiblePaths));
            return null;
        }

        /// <summary>
        /// Parses and validates supplemental table structure
        /// </summary>
        /// <param name="filePath">Path to supplemental table file</param>
        /// <returns>Parsed supplemental table data or null if parsing failed</returns>
        private async Task<SupplementalTableData?> ParseSupplementalTableAsync(string filePath)
        {
            try
            {
                _logger.Information("Parsing supplemental table file: {FilePath}", filePath);

                var supplementalData = await _parser.ParseFileAsync(filePath);
                
                _logger.Information("Successfully parsed supplemental table: {RecordCount} records", supplementalData.Count);
                
                // Log some statistics
                var defaultRecord = supplementalData.DefaultRecord;
                if (defaultRecord != null)
                {
                    _logger.Information("Default record found: {DefaultClient}", defaultRecord.ClientName);
                }

                // Validate that we have usable data
                if (supplementalData.Count == 0)
                {
                    _progressReporter.ReportStepError("Supplemental Table Parsing", 
                        "No valid records found in supplemental table");
                    return null;
                }

                _progressReporter.ReportStepCompleted("Supplemental Table Parsing", 
                    $"Parsed {supplementalData.Count} client records");

                return supplementalData;
            }
            catch (Exception ex)
            {
                _progressReporter.ReportStepError("Supplemental Table Parsing", 
                    $"Failed to parse supplemental table: {ex.Message}", ex);
                return null;
            }
        }

        /// <summary>
        /// Copies supplemental file to job-specific location (equivalent to cp command in script)
        /// </summary>
        /// <param name="jobNumber">Job number</param>
        /// <param name="sourceFilePath">Source supplemental table file path</param>
        /// <returns>Target file path or null if copy failed</returns>
        private async Task<string?> CopySupplementalFileAsync(string jobNumber, string sourceFilePath)
        {
            try
            {
                _logger.Information("Copying supplemental file for job {JobNumber}", jobNumber);

                var targetFilePath = _configuration.GetSupplementalTablePath(jobNumber);
                var targetDirectory = Path.GetDirectoryName(targetFilePath);

                // Ensure target directory exists
                if (!string.IsNullOrEmpty(targetDirectory))
                {
                    Directory.CreateDirectory(targetDirectory);
                }

                // Copy file (equivalent to: cp /users/programs/2503supptable.txt /users/public/$job.se1)
                await Task.Run(() => File.Copy(sourceFilePath, targetFilePath, overwrite: true));

                // Verify the copy was successful
                if (!File.Exists(targetFilePath))
                {
                    _progressReporter.ReportStepError("File Copy Operation", 
                        $"File copy failed - target file does not exist: {targetFilePath}");
                    return null;
                }

                var sourceInfo = new FileInfo(sourceFilePath);
                var targetInfo = new FileInfo(targetFilePath);

                if (sourceInfo.Length != targetInfo.Length)
                {
                    _progressReporter.ReportStepError("File Copy Operation", 
                        $"File copy validation failed - size mismatch. Source: {sourceInfo.Length}, Target: {targetInfo.Length}");
                    return null;
                }

                _logger.Information("Successfully copied supplemental file: {SourcePath} -> {TargetPath}", 
                    sourceFilePath, targetFilePath);
                
                _progressReporter.ReportStepCompleted("File Copy Operation", 
                    $"Copied to {Path.GetFileName(targetFilePath)} ({targetInfo.Length} bytes)");

                return targetFilePath;
            }
            catch (Exception ex)
            {
                _progressReporter.ReportStepError("File Copy Operation", 
                    $"Failed to copy supplemental file: {ex.Message}", ex);
                return null;
            }
        }

        /// <summary>
        /// Integrates with client configuration system
        /// </summary>
        /// <param name="arguments">Pipeline arguments</param>
        /// <param name="supplementalData">Parsed supplemental table data</param>
        /// <returns>True if integration successful</returns>
        private async Task<bool> IntegrateWithClientConfigurationAsync(PipelineArguments arguments, SupplementalTableData supplementalData)
        {
            await Task.Delay(10); // Simulate async work

            try
            {
                _logger.Information("Integrating with client configuration system");

                // In the legacy script, Client=2503 is hardcoded
                // This maps to the client configuration lookup
                var clientId = PipelineConfiguration.ClientDept.Substring(0, 4); // "2503" from "250301"
                
                var clientConfig = supplementalData.GetClientConfiguration(clientId);
                if (clientConfig == null)
                {
                    // Fallback to default configuration
                    clientConfig = supplementalData.DefaultRecord;
                    if (clientConfig == null)
                    {
                        _progressReporter.ReportStepError("Client Configuration Integration", 
                            $"No client configuration found for client {clientId} and no default record available");
                        return false;
                    }
                    
                    _progressReporter.ReportStepWarning("Client Configuration Integration", 
                        $"Client {clientId} not found, using default configuration: {clientConfig.ClientName}");
                }

                _logger.Information("Client configuration resolved: {ClientName} (ID: {ClientId})", 
                    clientConfig.ClientName, clientConfig.PLSClientId);

                // Log client-specific processing parameters
                _logger.Information("Client Configuration Details:");
                _logger.Information("  Client Name: {ClientName}", clientConfig.ClientName);
                _logger.Information("  Bank Number: {BankNumber}", clientConfig.BankNumber);
                _logger.Information("  Logo Name: {LogoName}", clientConfig.LogoName);
                _logger.Information("  Website: {Website}", clientConfig.Website);
                _logger.Information("  Time Zone: {TimeZone}", clientConfig.TimeZone);
                _logger.Information("  Client Nickname: {ClientNickname}", clientConfig.ClientNickname);

                // This configuration would be used by subsequent pipeline steps
                // For now, we just validate that we can access the configuration
                
                _progressReporter.ReportStepCompleted("Client Configuration Integration", 
                    $"Configuration loaded for {clientConfig.ClientName}");

                return true;
            }
            catch (Exception ex)
            {
                _progressReporter.ReportStepError("Client Configuration Integration", 
                    $"Failed to integrate with client configuration: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// Gets client configuration for a specific client ID
        /// This method can be used by other pipeline components
        /// </summary>
        /// <param name="jobNumber">Job number (for locating the supplemental file)</param>
        /// <param name="clientId">Client ID to look up</param>
        /// <returns>Client configuration or null if not found</returns>
        public async Task<SupplementalTableRecord?> GetClientConfigurationAsync(string jobNumber, string clientId)
        {
            try
            {
                var supplementalFilePath = _configuration.GetSupplementalTablePath(jobNumber);
                if (!File.Exists(supplementalFilePath))
                {
                    _logger.Warning("Supplemental file not found for job {JobNumber}: {FilePath}", jobNumber, supplementalFilePath);
                    return null;
                }

                var supplementalData = await _parser.ParseFileAsync(supplementalFilePath);
                return supplementalData.GetClientConfiguration(clientId);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to get client configuration for job {JobNumber}, client {ClientId}", jobNumber, clientId);
                return null;
            }
        }
    }
}
