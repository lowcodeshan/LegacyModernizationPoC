using LegacyModernization.Core.Configuration;
using LegacyModernization.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace LegacyModernization.Core.DataAccess
{
    /// <summary>
    /// Parser for supplemental table files (2503supptable.txt)
    /// Handles tab-delimited client configuration data
    /// </summary>
    public class SupplementalTableParser
    {
        private const char TAB_DELIMITER = '\t';
        private static readonly string[] ExpectedHeaders = new[]
        {
            "PLSCLIENTID", "BANKNUMBER", "CLIENTNAME", "LOGONAME", "WEBSITE",
            "RETURNSTREETADDRESS", "RETURNCITYADDRESS", "RETURNSTATEADDRESS", "RETURNZIPADDRESS",
            "PAYMENTNAME", "PAYMENTSTREETADDRESS", "PAYMENTCITYADDRESS", "PAYMENTSTATEADDRESS", "PAYMENTZIPADDRESS", "PAYMENTWEBSITE",
            "TOLLFREE#", "CUSTSERVPHONE#", "VIPCUSTSERVPHONE#", "COLLPHONE#", "VIPCOLLPHONE#", "ESCROWPHONE#", "PAYOFFPHONE#", "INSLSSPHONE#", "FAX#",
            "SERVICINGEMAIL", "DEFAULTEMAIL", "FCLEMAIL", "BNKEMAIL", "PAYOFFEMAIL", "ACQUISTIONSEMAIL", "CLAIMSEMAIL", "CONSTRUCTIONEMAIL",
            "EARLYINVEMAIL", "ESCROWEMAIL", "SPECIALIZEDLOANSEMAIL", "INSPECTIONSEMAIL", "INSLOSSEMAIL", "LOSSMITEMAIL", "NEWLOANSEMAIL", "SERVICERELEASEEMAIL",
            "TIMEZONE", "CUSTSERVSTARTDAY", "CUSTSERVENDDAY", "CUSTSERVSTARTTIME", "CUSTSERVENDTIME",
            "CUSTSERVSTARTDAY2", "CUSTSERVENDDAY2", "CUSTSERVSTARTTIME2", "CUSTSERVENDTIME2",
            "COLLSTARTDAY", "COLLENDDAY", "COLLSTARTTIME", "COLLENDTIME",
            "COLLSTARTDAY2", "COLLENDDAY2", "COLLSTARTTIME2", "COLLENDTIME2",
            "FOOTER", "ZONECODE", "CLIENTNICKNAME", "NMLS", "CREDITINQUIRIESEMAIL"
        };

        /// <summary>
        /// Parses supplemental table file asynchronously
        /// </summary>
        /// <param name="filePath">Path to the supplemental table file</param>
        /// <returns>Parsed supplemental table data</returns>
        public async Task<SupplementalTableData> ParseFileAsync(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Supplemental table file not found: {filePath}");

            var records = new List<SupplementalTableRecord>();
            var lines = await File.ReadAllLinesAsync(filePath);

            if (lines.Length == 0)
                throw new InvalidDataException("Supplemental table file is empty");

            // Parse header line
            var headerLine = lines[0];
            var headers = headerLine.Split(TAB_DELIMITER);
            
            ValidateHeaders(headers);

            // Parse data lines
            for (int i = 1; i < lines.Length; i++)
            {
                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line))
                    continue; // Skip empty lines

                try
                {
                    var record = ParseRecord(line, headers, i + 1);
                    if (record != null)
                    {
                        records.Add(record);
                    }
                }
                catch (Exception ex)
                {
                    throw new InvalidDataException($"Error parsing line {i + 1}: {ex.Message}", ex);
                }
            }

            if (records.Count == 0)
                throw new InvalidDataException("No valid records found in supplemental table file");

            return new SupplementalTableData(records);
        }

        /// <summary>
        /// Parses supplemental table file synchronously
        /// </summary>
        /// <param name="filePath">Path to the supplemental table file</param>
        /// <returns>Parsed supplemental table data</returns>
        public SupplementalTableData ParseFile(string filePath)
        {
            return ParseFileAsync(filePath).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Validates that all expected headers are present
        /// </summary>
        /// <param name="headers">Headers from the file</param>
        private static void ValidateHeaders(string[] headers)
        {
            if (headers.Length < ExpectedHeaders.Length)
            {
                throw new InvalidDataException($"Header count mismatch. Expected {ExpectedHeaders.Length}, found {headers.Length}");
            }

            // Check for critical headers
            var headerList = headers.Select(h => h.Trim().ToUpperInvariant()).ToList();
            var missingHeaders = ExpectedHeaders.Where(expected => 
                !headerList.Contains(expected.ToUpperInvariant())).ToList();

            if (missingHeaders.Any())
            {
                throw new InvalidDataException($"Missing required headers: {string.Join(", ", missingHeaders)}");
            }
        }

        /// <summary>
        /// Parses a single record line
        /// </summary>
        /// <param name="line">Data line to parse</param>
        /// <param name="headers">Column headers</param>
        /// <param name="lineNumber">Line number for error reporting</param>
        /// <returns>Parsed supplemental table record or null if line should be skipped</returns>
        private static SupplementalTableRecord? ParseRecord(string line, string[] headers, int lineNumber)
        {
            var fields = line.Split(TAB_DELIMITER);
            
            // Handle lines with fewer fields than headers (pad with empty strings)
            if (fields.Length < headers.Length)
            {
                var paddedFields = new string[headers.Length];
                Array.Copy(fields, paddedFields, fields.Length);
                for (int i = fields.Length; i < headers.Length; i++)
                {
                    paddedFields[i] = string.Empty;
                }
                fields = paddedFields;
            }

            var record = new SupplementalTableRecord();

            for (int i = 0; i < Math.Min(headers.Length, fields.Length); i++)
            {
                var header = headers[i].Trim().ToUpperInvariant();
                var value = CleanFieldValue(fields[i]);

                // Map fields to record properties
                switch (header)
                {
                    case "PLSCLIENTID":
                        record.PLSClientId = value;
                        break;
                    case "BANKNUMBER":
                        record.BankNumber = value;
                        break;
                    case "CLIENTNAME":
                        record.ClientName = value;
                        break;
                    case "LOGONAME":
                        record.LogoName = value;
                        break;
                    case "WEBSITE":
                        record.Website = value;
                        break;
                    case "RETURNSTREETADDRESS":
                        record.ReturnStreetAddress = value;
                        break;
                    case "RETURNCITYADDRESS":
                        record.ReturnCityAddress = value;
                        break;
                    case "RETURNSTATEADDRESS":
                        record.ReturnStateAddress = value;
                        break;
                    case "RETURNZIPADDRESS":
                        record.ReturnZipAddress = value;
                        break;
                    case "PAYMENTNAME":
                        record.PaymentName = value;
                        break;
                    case "PAYMENTSTREETADDRESS":
                        record.PaymentStreetAddress = value;
                        break;
                    case "PAYMENTCITYADDRESS":
                        record.PaymentCityAddress = value;
                        break;
                    case "PAYMENTSTATEADDRESS":
                        record.PaymentStateAddress = value;
                        break;
                    case "PAYMENTZIPADDRESS":
                        record.PaymentZipAddress = value;
                        break;
                    case "PAYMENTWEBSITE":
                        record.PaymentWebsite = value;
                        break;
                    case "TOLLFREE#":
                        record.TollFreeNumber = value;
                        break;
                    case "CUSTSERVPHONE#":
                        record.CustomerServicePhone = value;
                        break;
                    case "VIPCUSTSERVPHONE#":
                        record.VIPCustomerServicePhone = value;
                        break;
                    case "COLLPHONE#":
                        record.CollectionPhone = value;
                        break;
                    case "VIPCOLLPHONE#":
                        record.VIPCollectionPhone = value;
                        break;
                    case "ESCROWPHONE#":
                        record.EscrowPhone = value;
                        break;
                    case "PAYOFFPHONE#":
                        record.PayoffPhone = value;
                        break;
                    case "INSLSSPHONE#":
                        record.InsuranceLossPhone = value;
                        break;
                    case "FAX#":
                        record.FaxNumber = value;
                        break;
                    case "SERVICINGEMAIL":
                        record.ServicingEmail = value;
                        break;
                    case "DEFAULTEMAIL":
                        record.DefaultEmail = value;
                        break;
                    case "FCLEMAIL":
                        record.ForeclosureEmail = value;
                        break;
                    case "BNKEMAIL":
                        record.BankruptcyEmail = value;
                        break;
                    case "PAYOFFEMAIL":
                        record.PayoffEmail = value;
                        break;
                    case "ACQUISTIONSEMAIL":
                        record.AcquisitionsEmail = value;
                        break;
                    case "CLAIMSEMAIL":
                        record.ClaimsEmail = value;
                        break;
                    case "CONSTRUCTIONEMAIL":
                        record.ConstructionEmail = value;
                        break;
                    case "EARLYINVEMAIL":
                        record.EarlyInterventionEmail = value;
                        break;
                    case "ESCROWEMAIL":
                        record.EscrowEmail = value;
                        break;
                    case "SPECIALIZEDLOANSEMAIL":
                        record.SpecializedLoansEmail = value;
                        break;
                    case "INSPECTIONSEMAIL":
                        record.InspectionsEmail = value;
                        break;
                    case "INSLOSSEMAIL":
                        record.InsuranceLossEmail = value;
                        break;
                    case "LOSSMITEMAIL":
                        record.LossMitigationEmail = value;
                        break;
                    case "NEWLOANSEMAIL":
                        record.NewLoansEmail = value;
                        break;
                    case "SERVICERELEASEEMAIL":
                        record.ServiceReleaseEmail = value;
                        break;
                    case "TIMEZONE":
                        record.TimeZone = value;
                        break;
                    case "CUSTSERVSTARTDAY":
                        record.CustomerServiceStartDay = value;
                        break;
                    case "CUSTSERVENDDAY":
                        record.CustomerServiceEndDay = value;
                        break;
                    case "CUSTSERVSTARTTIME":
                        record.CustomerServiceStartTime = value;
                        break;
                    case "CUSTSERVENDTIME":
                        record.CustomerServiceEndTime = value;
                        break;
                    case "CUSTSERVSTARTDAY2":
                        record.CustomerServiceStartDay2 = value;
                        break;
                    case "CUSTSERVENDDAY2":
                        record.CustomerServiceEndDay2 = value;
                        break;
                    case "CUSTSERVSTARTTIME2":
                        record.CustomerServiceStartTime2 = value;
                        break;
                    case "CUSTSERVENDTIME2":
                        record.CustomerServiceEndTime2 = value;
                        break;
                    case "COLLSTARTDAY":
                        record.CollectionStartDay = value;
                        break;
                    case "COLLENDDAY":
                        record.CollectionEndDay = value;
                        break;
                    case "COLLSTARTTIME":
                        record.CollectionStartTime = value;
                        break;
                    case "COLLENDTIME":
                        record.CollectionEndTime = value;
                        break;
                    case "COLLSTARTDAY2":
                        record.CollectionStartDay2 = value;
                        break;
                    case "COLLENDDAY2":
                        record.CollectionEndDay2 = value;
                        break;
                    case "COLLSTARTTIME2":
                        record.CollectionStartTime2 = value;
                        break;
                    case "COLLENDTIME2":
                        record.CollectionEndTime2 = value;
                        break;
                    case "FOOTER":
                        record.Footer = value;
                        break;
                    case "ZONECODE":
                        record.ZoneCode = value;
                        break;
                    case "CLIENTNICKNAME":
                        record.ClientNickname = value;
                        break;
                    case "NMLS":
                        record.NMLS = value;
                        break;
                    case "CREDITINQUIRIESEMAIL":
                        record.CreditInquiriesEmail = value;
                        break;
                }
            }

            // Skip records that don't have minimum required data
            if (string.IsNullOrWhiteSpace(record.ClientName))
            {
                return null; // Skip empty or invalid records
            }

            return record;
        }

        /// <summary>
        /// Cleans and normalizes field values
        /// </summary>
        /// <param name="value">Raw field value</param>
        /// <returns>Cleaned field value</returns>
        private static string CleanFieldValue(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            // Remove quotes if present
            value = value.Trim();
            if (value.StartsWith("\"") && value.EndsWith("\"") && value.Length > 1)
            {
                value = value.Substring(1, value.Length - 2);
            }

            return value.Trim();
        }

        /// <summary>
        /// Validates that a supplemental table record has required fields
        /// </summary>
        /// <param name="record">Record to validate</param>
        /// <returns>Validation result</returns>
        public static ValidationResult ValidateRecord(SupplementalTableRecord record)
        {
            if (record == null)
                return ValidationResult.Failure("Record is null");

            if (!record.IsValid())
                return ValidationResult.Failure("Record is missing required fields (PLSClientId or ClientName)");

            return ValidationResult.Success();
        }
    }
}
