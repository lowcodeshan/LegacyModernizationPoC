using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Serilog;
using LegacyModernization.Core.Configuration;
using LegacyModernization.Core.Models;
using LegacyModernization.Core.Utilities;
using LegacyModernization.Core.Logging;

namespace LegacyModernization.Core.Pipeline
{
    /// <summary>
    /// E-bill Split Processing Implementation Component
    /// Equivalent to cnpsplit4.out execution and file movement operations (lines 56-61 of mbcntr2503.script)
    /// 
    /// Script equivalent:
    /// /users/programs/cnpsplit4.out 2000 1318 1 E 0 0 /users/public/$job'e.txt' ASCII /users/public/$job'p.asc'
    /// mv /users/public/$job'p.asc' /users/public/$job'p.asc.org'
    /// mv /users/public/$job'p.asc.match' /users/public/$job'e.asc'
    /// mv /users/public/$job'p.asc.unmatch' /users/public/$job'p.asc'
    /// </summary>
    public class EbillSplitComponent
    {
        private readonly ILogger _logger;
        private readonly ProgressReporter _progressReporter;
        private readonly PipelineConfiguration _configuration;

        // cnpsplit4.out parameters: 2000 1318 1 E 0 0
        private const int RECORD_LENGTH = 2000;
        private const int SPLIT_FIELD_OFFSET = 1318;
        private const int SPLIT_FIELD_LENGTH = 1;
        private const string ELECTRONIC_CRITERIA = "E";

        public EbillSplitComponent(
            ILogger logger, 
            ProgressReporter progressReporter,
            PipelineConfiguration configuration)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _progressReporter = progressReporter ?? throw new ArgumentNullException(nameof(progressReporter));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        /// <summary>
        /// Execute E-bill split processing
        /// Equivalent to cnpsplit4.out + mv operations from mbcntr2503.script
        /// </summary>
        /// <param name="arguments">Pipeline arguments</param>
        /// <returns>Processing success status</returns>
        public async Task<bool> ExecuteAsync(PipelineArguments arguments)
        {
            try
            {
                _progressReporter.ReportStep("E-bill Split Processing", "Starting E-bill split processing (cnpsplit4.out equivalent)", false);

                // Parse parameters equivalent to cnpsplit4.out parameter processing
                var splitParams = await ParseSplitParametersAsync(arguments);
                if (!splitParams.IsValid)
                {
                    _logger.Error("E-bill split parameter validation failed: {ErrorMessage}", splitParams.ErrorMessage);
                    return false;
                }

                _logger.Information("E-bill split parameters validated successfully: {Parameters}", splitParams);

                // Execute split processing equivalent to cnpsplit4.out
                var splitResult = await ProcessEbillSplitAsync(splitParams);
                if (!splitResult.Success)
                {
                    _logger.Error("E-bill split processing failed: {ErrorMessage}", splitResult.ErrorMessage);
                    return false;
                }

                // Execute file movements equivalent to mv commands
                var moveResult = await ProcessFileMovementsAsync(splitParams, splitResult);
                if (!moveResult.Success)
                {
                    _logger.Error("File movement operations failed: {ErrorMessage}", moveResult.ErrorMessage);
                    return false;
                }

                _logger.Information("E-bill split processing completed successfully: {Result}", splitResult);
                _progressReporter.ReportStep("E-bill Split Processing", "E-bill split processing completed successfully", true);

                return true;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "E-bill split processing failed with exception");
                _progressReporter.ReportStep("E-bill Split Processing", $"Failed: {ex.Message}", false);
                return false;
            }
        }

        /// <summary>
        /// Parse split parameters from pipeline arguments
        /// Equivalent to: cnpsplit4.out 2000 1318 1 E 0 0 parameter parsing
        /// </summary>
        /// <param name="arguments">Pipeline arguments</param>
        /// <returns>Parsed split parameters</returns>
        private async Task<EbillSplitParameters> ParseSplitParametersAsync(PipelineArguments arguments)
        {
            var splitParams = new EbillSplitParameters
            {
                JobNumber = arguments.JobNumber,
                RecordLength = RECORD_LENGTH,
                SplitFieldOffset = SPLIT_FIELD_OFFSET,
                SplitFieldLength = SPLIT_FIELD_LENGTH,
                ElectronicCriteria = ELECTRONIC_CRITERIA,
                
                // Input file is output from Task 2.4 (MB2000 conversion)
                InputFilePath = Path.Combine(_configuration.OutputPath, $"{arguments.JobNumber}p.asc"),
                
                // Output files as per cnpsplit4.out specification
                ElectronicBillsPath = Path.Combine(_configuration.OutputPath, $"{arguments.JobNumber}e.txt"),
                PaperBillsPath = Path.Combine(_configuration.OutputPath, $"{arguments.JobNumber}p.asc"),
                
                // Intermediate files for mv operations
                OriginalBackupPath = Path.Combine(_configuration.OutputPath, $"{arguments.JobNumber}p.asc.org"),
                MatchedRecordsPath = Path.Combine(_configuration.OutputPath, $"{arguments.JobNumber}p.asc.match"),
                UnmatchedRecordsPath = Path.Combine(_configuration.OutputPath, $"{arguments.JobNumber}p.asc.unmatch"),
                ElectronicAscPath = Path.Combine(_configuration.OutputPath, $"{arguments.JobNumber}e.asc")
            };

            // Validate input file exists (output from Task 2.4)
            if (!File.Exists(splitParams.InputFilePath))
            {
                splitParams.IsValid = false;
                splitParams.ErrorMessage = $"Input file not found: {splitParams.InputFilePath}";
                return splitParams;
            }

            // Validate input file size matches expected record structure
            var fileInfo = new FileInfo(splitParams.InputFilePath);
            if (fileInfo.Length % RECORD_LENGTH != 0)
            {
                _logger.Warning("Input file size ({FileSize}) is not a multiple of record length ({RecordLength})", 
                    fileInfo.Length, RECORD_LENGTH);
            }

            splitParams.IsValid = true;
            return splitParams;
        }

        /// <summary>
        /// Process E-bill split equivalent to cnpsplit4.out execution
        /// </summary>
        /// <param name="parameters">Split parameters</param>
        /// <returns>Split processing result</returns>
        private async Task<EbillSplitResult> ProcessEbillSplitAsync(EbillSplitParameters parameters)
        {
            var result = new EbillSplitResult
            {
                ProcessingStartTime = DateTime.Now
            };

            try
            {
                _progressReporter.ReportStep("E-bill Split Processing", "Reading input records", false);

                // Read input records from MB2000 conversion output
                var inputRecords = await ReadBinaryRecordsAsync(parameters.InputFilePath, parameters.RecordLength);
                result.InputRecordCount = inputRecords.Count;

                _logger.Information("Read {RecordCount} input records from {FilePath}", inputRecords.Count, parameters.InputFilePath);

                _progressReporter.ReportStep("E-bill Split Processing", "Splitting electronic vs paper bills", false);

                // Split records based on electronic criteria at specified field offset
                var splitRecords = SplitRecordsByElectronicCriteria(inputRecords, parameters);
                
                result.ElectronicRecordCount = splitRecords.ElectronicRecords.Count;
                result.PaperRecordCount = splitRecords.PaperRecords.Count;

                _logger.Information("Split complete: {ElectronicCount} electronic, {PaperCount} paper records", 
                    result.ElectronicRecordCount, result.PaperRecordCount);

                _progressReporter.ReportStep("E-bill Split Processing", "Writing split output files", false);

                // Write electronic bills output
                await WriteBinaryRecordsAsync(splitRecords.ElectronicRecords, parameters.ElectronicBillsPath);
                
                // Create temporary files for mv operations (matching legacy script behavior)
                await WriteBinaryRecordsAsync(splitRecords.ElectronicRecords, parameters.MatchedRecordsPath);
                await WriteBinaryRecordsAsync(splitRecords.PaperRecords, parameters.UnmatchedRecordsPath);

                result.Success = true;
                result.ProcessingEndTime = DateTime.Now;

                return result;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "E-bill split processing failed");
                result.Success = false;
                result.ErrorMessage = ex.Message;
                result.ProcessingEndTime = DateTime.Now;
                return result;
            }
        }

        /// <summary>
        /// Process file movements equivalent to mv commands from mbcntr2503.script
        /// </summary>
        /// <param name="parameters">Split parameters</param>
        /// <param name="splitResult">Split processing result</param>
        /// <returns>File movement result</returns>
        private async Task<FileMovementResult> ProcessFileMovementsAsync(EbillSplitParameters parameters, EbillSplitResult splitResult)
        {
            var result = new FileMovementResult
            {
                ProcessingStartTime = DateTime.Now
            };

            try
            {
                _progressReporter.ReportStep("E-bill Split Processing", "Executing file movements (mv equivalent)", false);

                var movements = new List<(string source, string destination, string description)>();

                // mv /users/public/$job'p.asc' /users/public/$job'p.asc.org'
                if (File.Exists(parameters.PaperBillsPath))
                {
                    movements.Add((parameters.PaperBillsPath, parameters.OriginalBackupPath, "Backup original p.asc to p.asc.org"));
                }

                // mv /users/public/$job'p.asc.match' /users/public/$job'e.asc'
                if (File.Exists(parameters.MatchedRecordsPath))
                {
                    movements.Add((parameters.MatchedRecordsPath, parameters.ElectronicAscPath, "Move matched records to e.asc"));
                }

                // mv /users/public/$job'p.asc.unmatch' /users/public/$job'p.asc'
                if (File.Exists(parameters.UnmatchedRecordsPath))
                {
                    movements.Add((parameters.UnmatchedRecordsPath, parameters.PaperBillsPath, "Move unmatched records to final p.asc"));
                }

                // Execute movements
                foreach (var (source, destination, description) in movements)
                {
                    _logger.Information("Executing file movement: {Description}", description);
                    _logger.Debug("Moving {Source} -> {Destination}", source, destination);

                    // Delete destination if it exists
                    if (File.Exists(destination))
                    {
                        File.Delete(destination);
                    }

                    // Move file
                    File.Move(source, destination);
                    result.MovementsExecuted++;

                    _logger.Information("File movement completed: {Source} -> {Destination}", source, destination);
                }

                result.Success = true;
                result.ProcessingEndTime = DateTime.Now;

                _logger.Information("All file movements completed successfully: {MovementCount} operations", result.MovementsExecuted);

                return result;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "File movement operations failed");
                result.Success = false;
                result.ErrorMessage = ex.Message;
                result.ProcessingEndTime = DateTime.Now;
                return result;
            }
        }

        /// <summary>
        /// Read binary records from file
        /// </summary>
        /// <param name="filePath">Input file path</param>
        /// <param name="recordLength">Expected record length</param>
        /// <returns>List of binary records</returns>
        private async Task<List<byte[]>> ReadBinaryRecordsAsync(string filePath, int recordLength)
        {
            var records = new List<byte[]>();
            
            using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            using var reader = new BinaryReader(fileStream);

            while (fileStream.Position < fileStream.Length)
            {
                var remainingBytes = fileStream.Length - fileStream.Position;
                if (remainingBytes < recordLength)
                {
                    _logger.Warning("Partial record detected at end of file: {RemainingBytes} bytes", remainingBytes);
                    break;
                }

                var recordData = reader.ReadBytes(recordLength);
                records.Add(recordData);
            }

            return records;
        }

        /// <summary>
        /// Split records based on electronic criteria
        /// Equivalent to cnpsplit4.out field analysis logic
        /// </summary>
        /// <param name="inputRecords">Input binary records</param>
        /// <param name="parameters">Split parameters</param>
        /// <returns>Split records result</returns>
        private SplitRecordsResult SplitRecordsByElectronicCriteria(List<byte[]> inputRecords, EbillSplitParameters parameters)
        {
            var result = new SplitRecordsResult
            {
                ElectronicRecords = new List<byte[]>(),
                PaperRecords = new List<byte[]>()
            };

            foreach (var record in inputRecords)
            {
                // Extract split field value at specified offset
                var splitFieldValue = ExtractSplitFieldValue(record, parameters.SplitFieldOffset, parameters.SplitFieldLength);
                
                // Check if record matches electronic criteria
                if (splitFieldValue.Equals(parameters.ElectronicCriteria, StringComparison.OrdinalIgnoreCase))
                {
                    result.ElectronicRecords.Add(record);
                    _logger.Debug("Record classified as electronic: split field = '{SplitField}'", splitFieldValue);
                }
                else
                {
                    result.PaperRecords.Add(record);
                    _logger.Debug("Record classified as paper: split field = '{SplitField}'", splitFieldValue);
                }
            }

            _logger.Information("Split classification complete: {ElectronicCount} electronic, {PaperCount} paper", 
                result.ElectronicRecords.Count, result.PaperRecords.Count);

            return result;
        }

        /// <summary>
        /// Extract split field value from binary record
        /// </summary>
        /// <param name="record">Binary record data</param>
        /// <param name="offset">Field offset</param>
        /// <param name="length">Field length</param>
        /// <returns>Split field value as string</returns>
        private string ExtractSplitFieldValue(byte[] record, int offset, int length)
        {
            if (offset + length > record.Length)
            {
                _logger.Warning("Split field extends beyond record boundary: offset={Offset}, length={Length}, recordSize={RecordSize}", 
                    offset, length, record.Length);
                return string.Empty;
            }

            var fieldBytes = new byte[length];
            Array.Copy(record, offset, fieldBytes, 0, length);
            
            // Convert to ASCII string for comparison
            return System.Text.Encoding.ASCII.GetString(fieldBytes).Trim();
        }

        /// <summary>
        /// Write binary records to file
        /// </summary>
        /// <param name="records">Binary records to write</param>
        /// <param name="filePath">Output file path</param>
        private async Task WriteBinaryRecordsAsync(List<byte[]> records, string filePath)
        {
            try
            {
                using var outputStream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
                
                foreach (var record in records)
                {
                    await outputStream.WriteAsync(record, 0, record.Length);
                }

                _logger.Information("Wrote {RecordCount} binary records ({TotalBytes} bytes) to {FilePath}", 
                    records.Count, records.Sum(r => r.Length), filePath);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to write binary records to {FilePath}", filePath);
                throw;
            }
        }
    }

    /// <summary>
    /// E-bill split parameters equivalent to cnpsplit4.out parameters
    /// </summary>
    public class EbillSplitParameters
    {
        /// <summary>
        /// Job number for the split operation
        /// </summary>
        public string JobNumber { get; set; } = string.Empty;

        /// <summary>
        /// Record length (2000 bytes)
        /// </summary>
        public int RecordLength { get; set; }

        /// <summary>
        /// Offset to split field (1318)
        /// </summary>
        public int SplitFieldOffset { get; set; }

        /// <summary>
        /// Length of split field (1 byte)
        /// </summary>
        public int SplitFieldLength { get; set; }

        /// <summary>
        /// Electronic criteria value ("E")
        /// </summary>
        public string ElectronicCriteria { get; set; } = string.Empty;

        /// <summary>
        /// Input file path (from MB2000 conversion)
        /// </summary>
        public string InputFilePath { get; set; } = string.Empty;

        /// <summary>
        /// Electronic bills output path
        /// </summary>
        public string ElectronicBillsPath { get; set; } = string.Empty;

        /// <summary>
        /// Paper bills output path
        /// </summary>
        public string PaperBillsPath { get; set; } = string.Empty;

        /// <summary>
        /// Original backup path (.asc.org)
        /// </summary>
        public string OriginalBackupPath { get; set; } = string.Empty;

        /// <summary>
        /// Matched records path (.asc.match)
        /// </summary>
        public string MatchedRecordsPath { get; set; } = string.Empty;

        /// <summary>
        /// Unmatched records path (.asc.unmatch)
        /// </summary>
        public string UnmatchedRecordsPath { get; set; } = string.Empty;

        /// <summary>
        /// Electronic ASC path (e.asc)
        /// </summary>
        public string ElectronicAscPath { get; set; } = string.Empty;

        /// <summary>
        /// Indicates if parameters are valid
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Error message if validation failed
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;
    }

    /// <summary>
    /// E-bill split processing result
    /// </summary>
    public class EbillSplitResult
    {
        /// <summary>
        /// Indicates if split was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Error message if split failed
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// Number of input records processed
        /// </summary>
        public int InputRecordCount { get; set; }

        /// <summary>
        /// Number of electronic records found
        /// </summary>
        public int ElectronicRecordCount { get; set; }

        /// <summary>
        /// Number of paper records found
        /// </summary>
        public int PaperRecordCount { get; set; }

        /// <summary>
        /// Processing start time
        /// </summary>
        public DateTime ProcessingStartTime { get; set; }

        /// <summary>
        /// Processing end time
        /// </summary>
        public DateTime ProcessingEndTime { get; set; }
    }

    /// <summary>
    /// File movement processing result
    /// </summary>
    public class FileMovementResult
    {
        /// <summary>
        /// Indicates if movements were successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Error message if movements failed
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// Number of file movements executed
        /// </summary>
        public int MovementsExecuted { get; set; }

        /// <summary>
        /// Processing start time
        /// </summary>
        public DateTime ProcessingStartTime { get; set; }

        /// <summary>
        /// Processing end time
        /// </summary>
        public DateTime ProcessingEndTime { get; set; }
    }

    /// <summary>
    /// Split records result
    /// </summary>
    public class SplitRecordsResult
    {
        /// <summary>
        /// Electronic billing records
        /// </summary>
        public List<byte[]> ElectronicRecords { get; set; } = new List<byte[]>();

        /// <summary>
        /// Paper billing records
        /// </summary>
        public List<byte[]> PaperRecords { get; set; } = new List<byte[]>();
    }
}
