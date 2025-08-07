using System;
using System.IO;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace LegacyModernization.Core.Validation
{
    /// <summary>
    /// Utilities for comparing binary files to validate output equivalence
    /// between legacy system and C# implementation
    /// </summary>
    public static class FileComparisonUtilities
    {
        /// <summary>
        /// Performs byte-by-byte comparison of two files
        /// </summary>
        /// <param name="expectedFilePath">Path to expected output file</param>
        /// <param name="actualFilePath">Path to actual output file</param>
        /// <returns>True if files are identical, false otherwise</returns>
        public static async Task<bool> AreFilesIdenticalAsync(string expectedFilePath, string actualFilePath)
        {
            if (!File.Exists(expectedFilePath))
                throw new FileNotFoundException($"Expected file not found: {expectedFilePath}");
            
            if (!File.Exists(actualFilePath))
                throw new FileNotFoundException($"Actual file not found: {actualFilePath}");

            var expectedInfo = new FileInfo(expectedFilePath);
            var actualInfo = new FileInfo(actualFilePath);

            // Quick size check first
            if (expectedInfo.Length != actualInfo.Length)
                return false;

            // For small files, do byte-by-byte comparison
            if (expectedInfo.Length < 1024 * 1024) // 1MB threshold
            {
                return await CompareFilesByteByByteAsync(expectedFilePath, actualFilePath);
            }
            
            // For larger files, use hash comparison first
            return await CompareFilesByHashAsync(expectedFilePath, actualFilePath);
        }

        /// <summary>
        /// Compares files using SHA256 hash
        /// </summary>
        private static async Task<bool> CompareFilesByHashAsync(string file1Path, string file2Path)
        {
            using var sha256 = SHA256.Create();
            
            byte[] hash1;
            using (var stream1 = File.OpenRead(file1Path))
            {
                hash1 = await sha256.ComputeHashAsync(stream1);
            }

            byte[] hash2;
            using (var stream2 = File.OpenRead(file2Path))
            {
                hash2 = await sha256.ComputeHashAsync(stream2);
            }

            return Convert.ToHexString(hash1).Equals(Convert.ToHexString(hash2), StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Compares files byte by byte
        /// </summary>
        private static async Task<bool> CompareFilesByteByByteAsync(string file1Path, string file2Path)
        {
            using var stream1 = File.OpenRead(file1Path);
            using var stream2 = File.OpenRead(file2Path);

            const int bufferSize = 4096;
            var buffer1 = new byte[bufferSize];
            var buffer2 = new byte[bufferSize];

            while (true)
            {
                var bytesRead1 = await stream1.ReadAsync(buffer1, 0, bufferSize);
                var bytesRead2 = await stream2.ReadAsync(buffer2, 0, bufferSize);

                if (bytesRead1 != bytesRead2)
                    return false;

                if (bytesRead1 == 0)
                    break; // End of both files

                for (int i = 0; i < bytesRead1; i++)
                {
                    if (buffer1[i] != buffer2[i])
                        return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Gets detailed comparison report showing differences between files
        /// </summary>
        /// <param name="expectedFilePath">Path to expected output file</param>
        /// <param name="actualFilePath">Path to actual output file</param>
        /// <returns>Detailed comparison report</returns>
        public static async Task<FileComparisonReport> GetDetailedComparisonAsync(string expectedFilePath, string actualFilePath)
        {
            var report = new FileComparisonReport
            {
                ExpectedFilePath = expectedFilePath,
                ActualFilePath = actualFilePath,
                ComparisonTimestamp = DateTime.UtcNow
            };

            try
            {
                if (!File.Exists(expectedFilePath))
                {
                    report.IsIdentical = false;
                    report.ErrorMessage = $"Expected file not found: {expectedFilePath}";
                    return report;
                }

                if (!File.Exists(actualFilePath))
                {
                    report.IsIdentical = false;
                    report.ErrorMessage = $"Actual file not found: {actualFilePath}";
                    return report;
                }

                var expectedInfo = new FileInfo(expectedFilePath);
                var actualInfo = new FileInfo(actualFilePath);

                report.ExpectedFileSize = expectedInfo.Length;
                report.ActualFileSize = actualInfo.Length;

                if (expectedInfo.Length != actualInfo.Length)
                {
                    report.IsIdentical = false;
                    report.ErrorMessage = $"File sizes differ: expected {expectedInfo.Length} bytes, actual {actualInfo.Length} bytes";
                    return report;
                }

                report.IsIdentical = await AreFilesIdenticalAsync(expectedFilePath, actualFilePath);
                
                if (!report.IsIdentical)
                {
                    // Find first difference for detailed reporting
                    report.FirstDifferencePosition = await FindFirstDifferenceAsync(expectedFilePath, actualFilePath);
                }
            }
            catch (Exception ex)
            {
                report.IsIdentical = false;
                report.ErrorMessage = $"Comparison failed: {ex.Message}";
            }

            return report;
        }

        /// <summary>
        /// Finds the byte position of the first difference between two files
        /// </summary>
        private static async Task<long?> FindFirstDifferenceAsync(string file1Path, string file2Path)
        {
            using var stream1 = File.OpenRead(file1Path);
            using var stream2 = File.OpenRead(file2Path);

            const int bufferSize = 4096;
            var buffer1 = new byte[bufferSize];
            var buffer2 = new byte[bufferSize];
            long position = 0;

            while (true)
            {
                var bytesRead1 = await stream1.ReadAsync(buffer1, 0, bufferSize);
                var bytesRead2 = await stream2.ReadAsync(buffer2, 0, bufferSize);

                if (bytesRead1 == 0 && bytesRead2 == 0)
                    break; // End of both files

                var minBytes = Math.Min(bytesRead1, bytesRead2);
                for (int i = 0; i < minBytes; i++)
                {
                    if (buffer1[i] != buffer2[i])
                        return position + i;
                }

                if (bytesRead1 != bytesRead2)
                    return position + minBytes;

                position += bytesRead1;
            }

            return null; // No differences found
        }
    }

    /// <summary>
    /// Report containing detailed comparison results
    /// </summary>
    public class FileComparisonReport
    {
        public string ExpectedFilePath { get; set; } = string.Empty;
        public string ActualFilePath { get; set; } = string.Empty;
        public bool IsIdentical { get; set; }
        public long ExpectedFileSize { get; set; }
        public long ActualFileSize { get; set; }
        public long? FirstDifferencePosition { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public DateTime ComparisonTimestamp { get; set; }

        public override string ToString()
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
                return $"Comparison Error: {ErrorMessage}";

            if (IsIdentical)
                return $"Files are identical ({ExpectedFileSize} bytes)";

            var result = $"Files differ (Expected: {ExpectedFileSize} bytes, Actual: {ActualFileSize} bytes)";
            if (FirstDifferencePosition.HasValue)
                result += $" - First difference at byte position {FirstDifferencePosition.Value}";

            return result;
        }
    }
}
