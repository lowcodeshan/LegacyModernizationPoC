using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace LegacyModernization.Core.Models
{
    /// <summary>
    /// Container parameters equivalent to ncpcntr5v2.script parameter structure
    /// Parameters: j-$job $InPath c-$Client 2-$Work2Len r-$Project e-$ProjectBase
    /// </summary>
    public class ContainerParameters
    {
        /// <summary>
        /// j- parameter: Job number
        /// </summary>
        [Required]
        public string JobNumber { get; set; } = string.Empty;

        /// <summary>
        /// Input path parameter: Path to the input .dat file
        /// </summary>
        [Required]
        public string InputPath { get; set; } = string.Empty;

        /// <summary>
        /// c- parameter: Client ID
        /// </summary>
        [Required]
        public string ClientId { get; set; } = string.Empty;

        /// <summary>
        /// 2- parameter: Work2 record length
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "Work2Length must be positive")]
        public int Work2Length { get; set; }

        /// <summary>
        /// r- parameter: Project type (e.g., "mblps")
        /// </summary>
        [Required]
        public string ProjectType { get; set; } = string.Empty;

        /// <summary>
        /// e- parameter: Project base path
        /// </summary>
        [Required]
        public string ProjectBasePath { get; set; } = string.Empty;

        /// <summary>
        /// Validation status
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Error message if validation fails
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// Additional processing options
        /// </summary>
        public Dictionary<string, string> ProcessingOptions { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Override the ToString method for logging purposes
        /// </summary>
        /// <returns>String representation of container parameters</returns>
        public override string ToString()
        {
            return $"ContainerParameters [Job: {JobNumber}, Client: {ClientId}, Work2Length: {Work2Length}, " +
                   $"Project: {ProjectType}, InputPath: {InputPath}, ProjectBase: {ProjectBasePath}]";
        }
    }

    /// <summary>
    /// Container record representing a single data record from the input file
    /// Based on COBOL data definitions in CONTAINER_LIBRARY/mblps/mblps.dd
    /// </summary>
    public class ContainerRecord
    {
        /// <summary>
        /// MB-CLIENT3: Client code (3 characters)
        /// </summary>
        public string ClientCode { get; set; } = string.Empty;

        /// <summary>
        /// MB-ACCOUNT: Account number (packed decimal)
        /// </summary>
        public string AccountNumber { get; set; } = string.Empty;

        /// <summary>
        /// MB-FORMATTED-ACCOUNT: Formatted account number (10 characters)
        /// </summary>
        public string FormattedAccount { get; set; } = string.Empty;

        /// <summary>
        /// MB-BILL-NAME: Bill name (60 characters)
        /// </summary>
        public string BillName { get; set; } = string.Empty;

        /// <summary>
        /// Work2 length applied during processing
        /// </summary>
        public int Work2Length { get; set; }

        /// <summary>
        /// Processing flags applied during transformation
        /// </summary>
        public List<string> ProcessingFlags { get; set; } = new List<string>();

        /// <summary>
        /// Raw binary data for fields not yet parsed
        /// </summary>
        public Dictionary<string, byte[]> RawFields { get; set; } = new Dictionary<string, byte[]>();

        /// <summary>
        /// Create a clone of this record for transformation processing
        /// </summary>
        /// <returns>Cloned container record</returns>
        public ContainerRecord Clone()
        {
            return new ContainerRecord
            {
                ClientCode = this.ClientCode,
                AccountNumber = this.AccountNumber,
                FormattedAccount = this.FormattedAccount,
                BillName = this.BillName,
                Work2Length = this.Work2Length,
                ProcessingFlags = new List<string>(this.ProcessingFlags),
                RawFields = new Dictionary<string, byte[]>(this.RawFields)
            };
        }

        /// <summary>
        /// Convert record to Work2 format for output file
        /// </summary>
        /// <returns>Work2 formatted string</returns>
        public string ToWork2Format()
        {
            // Generate Work2 format output line
            // This format is used for downstream processing
            var flags = ProcessingFlags.Any() ? $"[{string.Join(",", ProcessingFlags)}]" : "";
            
            return $"{ClientCode.PadRight(3)}\t{AccountNumber.PadLeft(10)}\t{FormattedAccount.PadRight(10)}\t" +
                   $"{BillName.PadRight(60)}\t{Work2Length}\t{flags}";
        }

        /// <summary>
        /// Override ToString for debugging and logging
        /// </summary>
        /// <returns>String representation of the record</returns>
        public override string ToString()
        {
            return $"ContainerRecord [Client: {ClientCode}, Account: {AccountNumber}, " +
                   $"FormattedAccount: {FormattedAccount}, BillName: {BillName?.Substring(0, Math.Min(20, BillName?.Length ?? 0))}...]";
        }
    }

    /// <summary>
    /// Result of container processing operation
    /// </summary>
    public class ContainerProcessingResult
    {
        /// <summary>
        /// Indicates if processing was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Error message if processing failed
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// Number of input records processed
        /// </summary>
        public int InputRecordCount { get; set; }

        /// <summary>
        /// Number of output records generated
        /// </summary>
        public int OutputRecordCount { get; set; }

        /// <summary>
        /// Path to the Work2 output file
        /// </summary>
        public string Work2OutputPath { get; set; } = string.Empty;

        /// <summary>
        /// Processing statistics and metrics
        /// </summary>
        public Dictionary<string, object> ProcessingMetrics { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Timestamp when processing started
        /// </summary>
        public DateTime ProcessingStartTime { get; set; }

        /// <summary>
        /// Timestamp when processing completed
        /// </summary>
        public DateTime ProcessingEndTime { get; set; }

        /// <summary>
        /// Total processing duration
        /// </summary>
        public TimeSpan ProcessingDuration => ProcessingEndTime - ProcessingStartTime;

        /// <summary>
        /// Create a successful result
        /// </summary>
        /// <param name="inputCount">Number of input records</param>
        /// <param name="outputCount">Number of output records</param>
        /// <param name="work2OutputPath">Path to Work2 output file</param>
        /// <returns>Successful processing result</returns>
        public static ContainerProcessingResult CreateSuccess(int inputCount, int outputCount, string work2OutputPath)
        {
            return new ContainerProcessingResult
            {
                Success = true,
                InputRecordCount = inputCount,
                OutputRecordCount = outputCount,
                Work2OutputPath = work2OutputPath,
                ProcessingEndTime = DateTime.Now
            };
        }

        /// <summary>
        /// Create a failed result
        /// </summary>
        /// <param name="errorMessage">Error message</param>
        /// <returns>Failed processing result</returns>
        public static ContainerProcessingResult CreateFailed(string errorMessage)
        {
            return new ContainerProcessingResult
            {
                Success = false,
                ErrorMessage = errorMessage,
                ProcessingEndTime = DateTime.Now
            };
        }

        /// <summary>
        /// Override ToString for logging purposes
        /// </summary>
        /// <returns>String representation of the result</returns>
        public override string ToString()
        {
            if (Success)
            {
                return $"ContainerProcessingResult [Success: {Success}, InputRecords: {InputRecordCount}, " +
                       $"OutputRecords: {OutputRecordCount}, Duration: {ProcessingDuration.TotalSeconds:F2}s]";
            }
            else
            {
                return $"ContainerProcessingResult [Success: {Success}, Error: {ErrorMessage}]";
            }
        }
    }
}
