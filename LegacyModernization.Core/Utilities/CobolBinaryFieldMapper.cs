using System;
using System.Text;
using LegacyModernization.Core.Models;

namespace LegacyModernization.Core.Utilities
{
    /// <summary>
    /// Maps MB2000OutputRecord fields to binary buffer using COBOL structure definitions
    /// </summary>
    public class CobolBinaryFieldMapper
    {
        private readonly MB2000RecordStructure _cobolStructure;

        public CobolBinaryFieldMapper(MB2000RecordStructure cobolStructure)
        {
            _cobolStructure = cobolStructure;
        }

        /// <summary>
        /// Map fields from MB2000OutputRecord to binary buffer using COBOL field positions
        /// </summary>
        public void MapFieldsToBuffer(MB2000OutputRecord record, byte[] buffer)
        {
            foreach (var field in _cobolStructure.Fields)
            {
                MapSingleField(record, buffer, field);
            }
        }

        private void MapSingleField(MB2000OutputRecord record, byte[] buffer, CobolFieldDefinition field)
        {
            try
            {
                switch (field.Name)
                {
                    case "MB-CLIENT3":
                        WriteAlphanumericField(buffer, field, record.Client.PadRight(3));
                        break;
                    case "MB-ACCOUNT":
                        // Account number is packed decimal - critical field for accuracy
                        WritePackedDecimalField(buffer, field, record.Account);
                        break;
                    case "MB-FORMATTED-ACCOUNT":
                        WriteAlphanumericField(buffer, field, record.Account.PadRight(10));
                        break;
                    case "MB-BILL-NAME":
                        WriteAlphanumericField(buffer, field, record.BillName?.PadRight(60) ?? new string(' ', 60));
                        break;
                    case "MB-BILL-LINE-2":
                        WriteAlphanumericField(buffer, field, record.BillLine2?.PadRight(60) ?? new string(' ', 60));
                        break;
                    case "MB-BILL-LINE-3":
                        WriteAlphanumericField(buffer, field, record.BillLine3?.PadRight(60) ?? new string(' ', 60));
                        break;
                    case "MB-BILL-CITY":
                        WriteAlphanumericField(buffer, field, record.BillCity?.PadRight(51) ?? new string(' ', 51));
                        break;
                    case "MB-BILL-STATE":
                        WriteAlphanumericField(buffer, field, record.BillState?.PadRight(2) ?? new string(' ', 2));
                        break;
                    case "MB-FIRST-PRIN-BAL":
                        // Financial amount with 2 decimal places - critical field for accuracy  
                        WritePackedDecimalField(buffer, field, record.PrincipalBalance.ToString("F2"));
                        break;
                    case "MB-PAYMENT-AMOUNT":
                        // Financial amount with 2 decimal places - critical field for accuracy
                        WritePackedDecimalField(buffer, field, record.PaymentAmount.ToString("F2"));
                        break;
                    case "MB-TRAN-KEY":
                        WritePackedDecimalField(buffer, field, record.TranKey ?? "0");
                        break;
                    case "MB-TRAN-COUNT":
                        WritePackedDecimalField(buffer, field, record.TranCount ?? "0");
                        break;
                    case "MB-JOB":
                        WriteAlphanumericField(buffer, field, record.Job?.PadRight(7) ?? new string(' ', 7));
                        break;
                    case "MB-CLIENT":
                        WriteAlphanumericField(buffer, field, record.Client.PadRight(3));
                        break;
                }
            }
            catch
            {
                // Ignore mapping errors for individual fields
            }
        }

        private void WriteAlphanumericField(byte[] buffer, CobolFieldDefinition field, string value)
        {
            if (field.Position <= 0 || field.Position + field.Length > buffer.Length) return;
            
            var bytes = Encoding.ASCII.GetBytes(value.Substring(0, Math.Min(value.Length, field.Length)));
            Array.Copy(bytes, 0, buffer, field.Position - 1, Math.Min(bytes.Length, field.Length));
        }

        private void WritePackedDecimalField(byte[] buffer, CobolFieldDefinition field, string value)
        {
            if (field.Position <= 0 || field.Position + field.Length > buffer.Length) return;
            
            // Enhanced packed decimal encoding with proper field size handling
            var packed = EncodePackedDecimal(value, field.Length, 2); // Default 2 decimal places
            Array.Copy(packed, 0, buffer, field.Position - 1, Math.Min(packed.Length, field.Length));
        }

        private byte[] EncodePackedDecimal(string value, int totalSize, int decimalPlaces)
        {
            // Remove any non-numeric characters except decimal point
            var cleanValue = value.Replace("$", "").Replace(",", "").Trim();
            
            // Parse decimal value
            if (!decimal.TryParse(cleanValue, out decimal decimalValue))
            {
                decimalValue = 0m;
            }
            
            // Use 2 decimal places as default for financial fields
            var actualDecimalPlaces = decimalPlaces > 0 ? decimalPlaces : 2;
            
            // Scale to integer representation (multiply by 10^decimalPlaces)
            var scaledValue = (long)(decimalValue * (decimal)Math.Pow(10, actualDecimalPlaces));
            
            // Convert to string with proper zero padding
            var digitString = Math.Abs(scaledValue).ToString();
            var totalDigits = totalSize * 2 - 1; // Each byte holds 2 digits except last has sign
            digitString = digitString.PadLeft(totalDigits, '0');
            
            // Create packed decimal bytes
            var packed = new byte[totalSize];
            
            // Pack digits (2 per byte, except last)
            for (int i = 0; i < totalSize - 1; i++)
            {
                var digit1 = digitString[i * 2] - '0';
                var digit2 = digitString[i * 2 + 1] - '0';
                packed[i] = (byte)((digit1 << 4) | digit2);
            }
            
            // Last byte: final digit + sign
            var lastDigit = digitString[digitString.Length - 1] - '0';
            var sign = scaledValue >= 0 ? 0x0C : 0x0D; // C=positive, D=negative
            packed[totalSize - 1] = (byte)((lastDigit << 4) | sign);
            
            return packed;
        }
    }
}
