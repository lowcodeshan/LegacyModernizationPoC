using System;
using System.Text;

namespace LegacyModernization.Core.Utilities
{
    /// <summary>
    /// EBCDIC to ASCII conversion utility for legacy mainframe data processing
    /// Handles the conversion of EBCDIC encoded binary data to ASCII for proper field extraction
    /// </summary>
    public static class EbcdicConverter
    {
        /// <summary>
        /// EBCDIC to ASCII conversion table
        /// Maps EBCDIC byte values (0x00-0xFF) to corresponding ASCII characters
        /// </summary>
        private static readonly char[] EbcdicToAsciiTable = new char[256]
        {
            // 0x00-0x0F
            '\0', '\x01', '\x02', '\x03', '\x9C', '\t', '\x86', '\x7F',
            '\x97', '\x8D', '\x8E', '\x0B', '\f', '\r', '\x0E', '\x0F',
            // 0x10-0x1F  
            '\x10', '\x11', '\x12', '\x13', '\x9D', '\x85', '\x08', '\x87',
            '\x18', '\x19', '\x92', '\x8F', '\x1C', '\x1D', '\x1E', '\x1F',
            // 0x20-0x2F
            '\x80', '\x81', '\x82', '\x83', '\x84', '\n', '\x17', '\x1B',
            '\x88', '\x89', '\x8A', '\x8B', '\x8C', '\x05', '\x06', '\x07',
            // 0x30-0x3F
            '\x90', '\x91', '\x16', '\x93', '\x94', '\x95', '\x96', '\x04',
            '\x98', '\x99', '\x9A', '\x9B', '\x14', '\x15', '\x9E', '\x1A',
            // 0x40-0x4F (spaces and punctuation)
            ' ', '\xA0', '\xE2', '\xE4', '\xE0', '\xE1', '\xE3', '\xE5',
            '\xE7', '\xF1', '\xA2', '.', '<', '(', '+', '|',
            // 0x50-0x5F
            '&', '\xE9', '\xEA', '\xEB', '\xE8', '\xED', '\xEE', '\xEF',
            '\xEC', '\xDF', '!', '$', '*', ')', ';', '\xAC',
            // 0x60-0x6F
            '-', '/', '\xC2', '\xC4', '\xC0', '\xC1', '\xC3', '\xC5',
            '\xC7', '\xD1', '\xA6', ',', '%', '_', '>', '?',
            // 0x70-0x7F
            '\xF8', '\xC9', '\xCA', '\xCB', '\xC8', '\xCD', '\xCE', '\xCF',
            '\xCC', '`', ':', '#', '@', '\'', '=', '"',
            // 0x80-0x8F
            '\xD8', 'a', 'b', 'c', 'd', 'e', 'f', 'g',
            'h', 'i', '\xAB', '\xBB', '\xF0', '\xFD', '\xFE', '\xB1',
            // 0x90-0x9F
            '\xB0', 'j', 'k', 'l', 'm', 'n', 'o', 'p',
            'q', 'r', '\xAA', '\xBA', '\xE6', '\xB8', '\xC6', '\xA4',
            // 0xA0-0xAF
            '\xB5', '~', 's', 't', 'u', 'v', 'w', 'x',
            'y', 'z', '\xA1', '\xBF', '\xD0', '\xDD', '\xDE', '\xAE',
            // 0xB0-0xBF
            '^', '\xA3', '\xA5', '\xB7', '\xA9', '\xA7', '\xB6', '\xBC',
            '\xBD', '\xBE', '[', ']', '\xAF', '\xA8', '\xB4', '\xD7',
            // 0xC0-0xCF
            '{', 'A', 'B', 'C', 'D', 'E', 'F', 'G',
            'H', 'I', '\xAD', '\xF4', '\xF6', '\xF2', '\xF3', '\xF5',
            // 0xD0-0xDF
            '}', 'J', 'K', 'L', 'M', 'N', 'O', 'P',
            'Q', 'R', '\xB9', '\xFB', '\xFC', '\xF9', '\xFA', '\xFF',
            // 0xE0-0xEF
            '\\', '\xF7', 'S', 'T', 'U', 'V', 'W', 'X',
            'Y', 'Z', '\xB2', '\xD4', '\xD6', '\xD2', '\xD3', '\xD5',
            // 0xF0-0xFF (digits)
            '0', '1', '2', '3', '4', '5', '6', '7',
            '8', '9', '\xB3', '\xDB', '\xDC', '\xD9', '\xDA', '\x9F'
        };

        /// <summary>
        /// Convert EBCDIC byte array to ASCII string
        /// </summary>
        /// <param name="ebcdicBytes">EBCDIC encoded byte array</param>
        /// <returns>ASCII string</returns>
        public static string ConvertToAscii(byte[] ebcdicBytes)
        {
            if (ebcdicBytes == null || ebcdicBytes.Length == 0)
                return string.Empty;

            var result = new StringBuilder(ebcdicBytes.Length);
            
            foreach (byte b in ebcdicBytes)
            {
                result.Append(EbcdicToAsciiTable[b]);
            }
            
            return result.ToString();
        }

        /// <summary>
        /// Convert a portion of EBCDIC byte array to ASCII string
        /// </summary>
        /// <param name="ebcdicBytes">EBCDIC encoded byte array</param>
        /// <param name="offset">Starting offset in the array</param>
        /// <param name="length">Number of bytes to convert</param>
        /// <returns>ASCII string</returns>
        public static string ConvertToAscii(byte[] ebcdicBytes, int offset, int length)
        {
            if (ebcdicBytes == null || offset < 0 || length <= 0 || offset + length > ebcdicBytes.Length)
                return string.Empty;

            var result = new StringBuilder(length);
            
            for (int i = offset; i < offset + length; i++)
            {
                result.Append(EbcdicToAsciiTable[ebcdicBytes[i]]);
            }
            
            return result.ToString();
        }

        /// <summary>
        /// Convert EBCDIC packed decimal to integer
        /// Packed decimal format: each byte contains two decimal digits, except the last byte
        /// where the rightmost nibble contains the sign (C=positive, D=negative)
        /// </summary>
        /// <param name="packedBytes">Packed decimal bytes</param>
        /// <returns>Integer value</returns>
        public static long ConvertPackedDecimal(byte[] packedBytes)
        {
            if (packedBytes == null || packedBytes.Length == 0)
                return 0;

            long result = 0;
            bool isNegative = false;

            // Process all bytes except the last one
            for (int i = 0; i < packedBytes.Length - 1; i++)
            {
                byte b = packedBytes[i];
                int highNibble = (b >> 4) & 0x0F;
                int lowNibble = b & 0x0F;
                
                result = result * 100 + highNibble * 10 + lowNibble;
            }

            // Process the last byte (contains sign)
            if (packedBytes.Length > 0)
            {
                byte lastByte = packedBytes[packedBytes.Length - 1];
                int digit = (lastByte >> 4) & 0x0F;
                int sign = lastByte & 0x0F;
                
                result = result * 10 + digit;
                isNegative = (sign == 0x0D); // D = negative
            }

            return isNegative ? -result : result;
        }

        /// <summary>
        /// Extract and convert EBCDIC field from byte array
        /// </summary>
        /// <param name="data">Source byte array</param>
        /// <param name="offset">Field offset</param>
        /// <param name="length">Field length</param>
        /// <param name="trimSpaces">Whether to trim trailing spaces</param>
        /// <returns>Converted ASCII string</returns>
        public static string ExtractField(byte[] data, int offset, int length, bool trimSpaces = true)
        {
            var field = ConvertToAscii(data, offset, length);
            return trimSpaces ? field.TrimEnd() : field;
        }
    }
}
