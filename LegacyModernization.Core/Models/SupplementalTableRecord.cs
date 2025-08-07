using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LegacyModernization.Core.Models
{
    /// <summary>
    /// Data model for supplemental table records (2503supptable.txt)
    /// Represents client configuration data used for bill processing
    /// </summary>
    public class SupplementalTableRecord
    {
        [Required]
        public string PLSClientId { get; set; } = string.Empty;
        
        public string BankNumber { get; set; } = string.Empty;
        
        [Required]
        public string ClientName { get; set; } = string.Empty;
        
        public string LogoName { get; set; } = string.Empty;
        
        public string Website { get; set; } = string.Empty;
        
        // Return Address Information
        public string ReturnStreetAddress { get; set; } = string.Empty;
        public string ReturnCityAddress { get; set; } = string.Empty;
        public string ReturnStateAddress { get; set; } = string.Empty;
        public string ReturnZipAddress { get; set; } = string.Empty;
        
        // Payment Address Information
        public string PaymentName { get; set; } = string.Empty;
        public string PaymentStreetAddress { get; set; } = string.Empty;
        public string PaymentCityAddress { get; set; } = string.Empty;
        public string PaymentStateAddress { get; set; } = string.Empty;
        public string PaymentZipAddress { get; set; } = string.Empty;
        public string PaymentWebsite { get; set; } = string.Empty;
        
        // Phone Numbers
        public string TollFreeNumber { get; set; } = string.Empty;
        public string CustomerServicePhone { get; set; } = string.Empty;
        public string VIPCustomerServicePhone { get; set; } = string.Empty;
        public string CollectionPhone { get; set; } = string.Empty;
        public string VIPCollectionPhone { get; set; } = string.Empty;
        public string EscrowPhone { get; set; } = string.Empty;
        public string PayoffPhone { get; set; } = string.Empty;
        public string InsuranceLossPhone { get; set; } = string.Empty;
        public string FaxNumber { get; set; } = string.Empty;
        
        // Email Addresses
        public string ServicingEmail { get; set; } = string.Empty;
        public string DefaultEmail { get; set; } = string.Empty;
        public string ForeclosureEmail { get; set; } = string.Empty;
        public string BankruptcyEmail { get; set; } = string.Empty;
        public string PayoffEmail { get; set; } = string.Empty;
        public string AcquisitionsEmail { get; set; } = string.Empty;
        public string ClaimsEmail { get; set; } = string.Empty;
        public string ConstructionEmail { get; set; } = string.Empty;
        public string EarlyInterventionEmail { get; set; } = string.Empty;
        public string EscrowEmail { get; set; } = string.Empty;
        public string SpecializedLoansEmail { get; set; } = string.Empty;
        public string InspectionsEmail { get; set; } = string.Empty;
        public string InsuranceLossEmail { get; set; } = string.Empty;
        public string LossMitigationEmail { get; set; } = string.Empty;
        public string NewLoansEmail { get; set; } = string.Empty;
        public string ServiceReleaseEmail { get; set; } = string.Empty;
        
        // Business Hours and Time Zone
        public string TimeZone { get; set; } = string.Empty;
        public string CustomerServiceStartDay { get; set; } = string.Empty;
        public string CustomerServiceEndDay { get; set; } = string.Empty;
        public string CustomerServiceStartTime { get; set; } = string.Empty;
        public string CustomerServiceEndTime { get; set; } = string.Empty;
        public string CustomerServiceStartDay2 { get; set; } = string.Empty;
        public string CustomerServiceEndDay2 { get; set; } = string.Empty;
        public string CustomerServiceStartTime2 { get; set; } = string.Empty;
        public string CustomerServiceEndTime2 { get; set; } = string.Empty;
        public string CollectionStartDay { get; set; } = string.Empty;
        public string CollectionEndDay { get; set; } = string.Empty;
        public string CollectionStartTime { get; set; } = string.Empty;
        public string CollectionEndTime { get; set; } = string.Empty;
        public string CollectionStartDay2 { get; set; } = string.Empty;
        public string CollectionEndDay2 { get; set; } = string.Empty;
        public string CollectionStartTime2 { get; set; } = string.Empty;
        public string CollectionEndTime2 { get; set; } = string.Empty;
        
        // Additional Information
        public string Footer { get; set; } = string.Empty;
        public string ZoneCode { get; set; } = string.Empty;
        public string ClientNickname { get; set; } = string.Empty;
        public string NMLS { get; set; } = string.Empty;
        public string CreditInquiriesEmail { get; set; } = string.Empty;
        
        /// <summary>
        /// Validates required fields for supplemental table record
        /// </summary>
        /// <returns>True if all required fields are present</returns>
        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(PLSClientId) && 
                   !string.IsNullOrWhiteSpace(ClientName);
        }
        
        /// <summary>
        /// Gets formatted return address
        /// </summary>
        /// <returns>Complete return address string</returns>
        public string GetFormattedReturnAddress()
        {
            var parts = new List<string>();
            
            if (!string.IsNullOrWhiteSpace(ReturnStreetAddress))
                parts.Add(ReturnStreetAddress.Trim());
            
            var cityStateZip = new List<string>();
            if (!string.IsNullOrWhiteSpace(ReturnCityAddress))
                cityStateZip.Add(ReturnCityAddress.Trim());
            if (!string.IsNullOrWhiteSpace(ReturnStateAddress))
                cityStateZip.Add(ReturnStateAddress.Trim());
            if (!string.IsNullOrWhiteSpace(ReturnZipAddress))
                cityStateZip.Add(ReturnZipAddress.Trim());
            
            if (cityStateZip.Count > 0)
                parts.Add(string.Join(" ", cityStateZip));
            
            return string.Join("\n", parts);
        }
        
        /// <summary>
        /// Gets formatted payment address
        /// </summary>
        /// <returns>Complete payment address string</returns>
        public string GetFormattedPaymentAddress()
        {
            var parts = new List<string>();
            
            if (!string.IsNullOrWhiteSpace(PaymentName))
                parts.Add(PaymentName.Trim());
            if (!string.IsNullOrWhiteSpace(PaymentStreetAddress))
                parts.Add(PaymentStreetAddress.Trim());
            
            var cityStateZip = new List<string>();
            if (!string.IsNullOrWhiteSpace(PaymentCityAddress))
                cityStateZip.Add(PaymentCityAddress.Trim());
            if (!string.IsNullOrWhiteSpace(PaymentStateAddress))
                cityStateZip.Add(PaymentStateAddress.Trim());
            if (!string.IsNullOrWhiteSpace(PaymentZipAddress))
                cityStateZip.Add(PaymentZipAddress.Trim());
            
            if (cityStateZip.Count > 0)
                parts.Add(string.Join(" ", cityStateZip));
            
            return string.Join("\n", parts);
        }
        
        /// <summary>
        /// Checks if this record is a default/fallback record
        /// </summary>
        /// <returns>True if this is a default record (empty PLSClientId or "All Others")</returns>
        public bool IsDefaultRecord()
        {
            return string.IsNullOrWhiteSpace(PLSClientId) || 
                   PLSClientId.Trim().Equals("All Others", StringComparison.OrdinalIgnoreCase);
        }
        
        public override string ToString()
        {
            return $"Client: {ClientName} (ID: {PLSClientId}, Bank: {BankNumber})";
        }
    }
    
    /// <summary>
    /// Container for supplemental table data with lookup capabilities
    /// </summary>
    public class SupplementalTableData
    {
        private readonly List<SupplementalTableRecord> _records;
        private readonly Dictionary<string, SupplementalTableRecord> _clientLookup;
        private readonly Dictionary<string, SupplementalTableRecord> _bankLookup;
        private SupplementalTableRecord? _defaultRecord;
        
        public SupplementalTableData(IEnumerable<SupplementalTableRecord> records)
        {
            _records = new List<SupplementalTableRecord>(records);
            _clientLookup = new Dictionary<string, SupplementalTableRecord>(StringComparer.OrdinalIgnoreCase);
            _bankLookup = new Dictionary<string, SupplementalTableRecord>(StringComparer.OrdinalIgnoreCase);
            
            BuildLookupTables();
        }
        
        /// <summary>
        /// Gets all records
        /// </summary>
        public IReadOnlyList<SupplementalTableRecord> Records => _records.AsReadOnly();
        
        /// <summary>
        /// Gets client configuration by PLS Client ID
        /// </summary>
        /// <param name="clientId">PLS Client ID</param>
        /// <returns>Client record or default record if not found</returns>
        public SupplementalTableRecord? GetClientConfiguration(string clientId)
        {
            if (string.IsNullOrWhiteSpace(clientId))
                return _defaultRecord;
            
            if (_clientLookup.TryGetValue(clientId.Trim(), out var record))
                return record;
            
            return _defaultRecord;
        }
        
        /// <summary>
        /// Gets client configuration by bank number
        /// </summary>
        /// <param name="bankNumber">Bank number</param>
        /// <returns>Client record or default record if not found</returns>
        public SupplementalTableRecord? GetClientConfigurationByBank(string bankNumber)
        {
            if (string.IsNullOrWhiteSpace(bankNumber))
                return _defaultRecord;
            
            if (_bankLookup.TryGetValue(bankNumber.Trim(), out var record))
                return record;
            
            return _defaultRecord;
        }
        
        /// <summary>
        /// Gets the default/fallback record
        /// </summary>
        public SupplementalTableRecord? DefaultRecord => _defaultRecord;
        
        /// <summary>
        /// Total number of records
        /// </summary>
        public int Count => _records.Count;
        
        private void BuildLookupTables()
        {
            foreach (var record in _records)
            {
                // Build client ID lookup
                if (!string.IsNullOrWhiteSpace(record.PLSClientId) && !record.IsDefaultRecord())
                {
                    _clientLookup[record.PLSClientId.Trim()] = record;
                }
                
                // Build bank number lookup
                if (!string.IsNullOrWhiteSpace(record.BankNumber))
                {
                    _bankLookup[record.BankNumber.Trim()] = record;
                }
                
                // Identify default record
                if (record.IsDefaultRecord())
                {
                    _defaultRecord = record;
                }
            }
        }
    }
}
