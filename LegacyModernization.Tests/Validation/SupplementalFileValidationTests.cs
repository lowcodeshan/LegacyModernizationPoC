using FluentAssertions;
using LegacyModernization.Core.Configuration;
using LegacyModernization.Core.DataAccess;
using LegacyModernization.Core.Logging;
using LegacyModernization.Core.Pipeline;
using Serilog;
using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Xunit;

namespace LegacyModernization.Tests.Validation
{
    /// <summary>
    /// Integration tests for supplemental file processing that validate
    /// output matches expected legacy system output
    /// </summary>
    public class SupplementalFileValidationTests : IDisposable
    {
        private readonly string _testOutputDirectory;
        private readonly string _expectedOutputDirectory;
        private readonly PipelineConfiguration _configuration;
        private readonly ILogger _logger;
        private readonly ProgressReporter _progressReporter;

        public SupplementalFileValidationTests()
        {
            // Use project-relative directories instead of temp to avoid permission issues
            var assemblyLocation = Assembly.GetExecutingAssembly().Location;
            var projectRoot = Path.GetDirectoryName(assemblyLocation)!;
            
            // Navigate up to solution root: bin/Debug/net8.0 -> project -> solution
            var solutionRoot = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(projectRoot))))!;
            
            _testOutputDirectory = Path.Combine(solutionRoot, "TestOutput", Guid.NewGuid().ToString("N")[..8]);
            _expectedOutputDirectory = Path.Combine(Path.GetDirectoryName(solutionRoot)!, "MBCNTR2053_Expected_Output");
            
            Directory.CreateDirectory(_testOutputDirectory);

            // Setup test configuration using existing project TestData directory
            _configuration = new PipelineConfiguration
            {
                ProjectBase = solutionRoot,
                InputPath = Path.Combine(solutionRoot, "TestData"),
                OutputPath = _testOutputDirectory,
                LogPath = Path.Combine(_testOutputDirectory, "Logs"),
                TestDataPath = Path.Combine(solutionRoot, "TestData"), // Use existing TestData directory
                ExpectedOutputPath = _expectedOutputDirectory
            };

            // Ensure directories exist and verify source file
            Directory.CreateDirectory(_configuration.LogPath);
            
            var sourceSupplementalFile = Path.Combine(_configuration.TestDataPath, "2503supptable.txt");
            if (!File.Exists(sourceSupplementalFile))
            {
                throw new FileNotFoundException($"Test data file not found: {sourceSupplementalFile}");
            }

            // Setup logger
            _logger = new Serilog.LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .CreateLogger();

            _progressReporter = new ProgressReporter(_logger, true);
        }

        [Fact]
        public async Task SupplementalFileProcessing_ShouldProduceIdenticalOutputToLegacySystem()
        {
            // Arrange
            const string jobNumber = "69172";
            var expectedOutputFile = Path.Combine(_expectedOutputDirectory, $"{jobNumber}.se1");
            var actualOutputFile = _configuration.GetSupplementalTablePath(jobNumber);

            // Skip test if expected output file doesn't exist
            if (!File.Exists(expectedOutputFile))
            {
                Assert.Fail($"Expected output file not found: {expectedOutputFile}");
                return;
            }

            var arguments = new PipelineArguments
            {
                JobNumber = jobNumber,
                Verbose = true,
                LogLevel = "Debug"
            };

            var supplementalComponent = new SupplementalFileProcessingComponent(_logger, _progressReporter, _configuration);

            // Act
            var result = await supplementalComponent.ExecuteAsync(arguments);

            // Assert
            result.Should().BeTrue("Supplemental file processing should succeed");
            File.Exists(actualOutputFile).Should().BeTrue($"Output file should exist: {actualOutputFile}");

            // Validate file size matches
            var expectedFileInfo = new FileInfo(expectedOutputFile);
            var actualFileInfo = new FileInfo(actualOutputFile);
            
            actualFileInfo.Length.Should().Be(expectedFileInfo.Length, 
                "Generated supplemental file size should match expected size");

            // Validate content matches byte-for-byte
            var expectedBytes = await File.ReadAllBytesAsync(expectedOutputFile);
            var actualBytes = await File.ReadAllBytesAsync(actualOutputFile);

            actualBytes.Should().BeEquivalentTo(expectedBytes, 
                "Generated supplemental file content should match expected content exactly");

            // Validate checksums match
            var expectedChecksum = ComputeSHA256Hash(expectedOutputFile);
            var actualChecksum = ComputeSHA256Hash(actualOutputFile);

            actualChecksum.Should().Be(expectedChecksum, 
                "Generated supplemental file checksum should match expected checksum");
        }

        [Fact]
        public async Task SupplementalTableParser_ShouldParseExpectedOutputFile()
        {
            // Arrange
            const string jobNumber = "69172";
            var expectedOutputFile = Path.Combine(_expectedOutputDirectory, $"{jobNumber}.se1");

            // Skip test if expected output file doesn't exist
            if (!File.Exists(expectedOutputFile))
            {
                Assert.Fail($"Expected output file not found: {expectedOutputFile}");
                return;
            }

            var parser = new SupplementalTableParser();

            // Act
            var result = await parser.ParseFileAsync(expectedOutputFile);

            // Assert
            result.Should().NotBeNull("Parser should successfully parse the expected output file");
            result.Count.Should().BeGreaterThan(0, "Expected output file should contain client records");
            result.DefaultRecord.Should().NotBeNull("Expected output file should contain a default record");

            // Validate specific expected data
            result.DefaultRecord!.ClientName.Should().NotBeNullOrEmpty("Default record should have a client name");
            
            // Log statistics for verification
            _logger.Information("Parsed expected output file: {RecordCount} records", result.Count);
            _logger.Information("Default record: {ClientName}", result.DefaultRecord.ClientName);
        }

        [Theory]
        [InlineData("69172")]
        [InlineData("12345")] // Test with different job number
        public async Task SupplementalFileProcessing_ShouldGenerateCorrectFileName(string jobNumber)
        {
            // Arrange
            var arguments = new PipelineArguments
            {
                JobNumber = jobNumber,
                Verbose = false,
                LogLevel = "Information"
            };

            var supplementalComponent = new SupplementalFileProcessingComponent(_logger, _progressReporter, _configuration);
            var expectedOutputFile = _configuration.GetSupplementalTablePath(jobNumber);

            // Act
            var result = await supplementalComponent.ExecuteAsync(arguments);

            // Assert
            result.Should().BeTrue($"Supplemental file processing should succeed for job {jobNumber}");
            File.Exists(expectedOutputFile).Should().BeTrue($"Output file should exist: {expectedOutputFile}");
            
            var actualFileName = Path.GetFileName(expectedOutputFile);
            var expectedFileName = $"{jobNumber}.se1";
            
            actualFileName.Should().Be(expectedFileName, 
                $"Generated file should have correct naming convention");
        }

        [Fact]
        public async Task SupplementalFileProcessing_ShouldValidateSourceFileIntegrity()
        {
            // Arrange
            const string jobNumber = "69172";
            var arguments = new PipelineArguments
            {
                JobNumber = jobNumber,
                Verbose = true,
                LogLevel = "Debug"
            };

            var supplementalComponent = new SupplementalFileProcessingComponent(_logger, _progressReporter, _configuration);
            
            // Act
            var result = await supplementalComponent.ExecuteAsync(arguments);

            // Assert
            result.Should().BeTrue("Supplemental file processing should succeed");

            var outputFile = _configuration.GetSupplementalTablePath(jobNumber);
            var sourceFile = Path.Combine(_configuration.TestDataPath, "2503supptable.txt");

            // Validate source and output files have same content
            if (File.Exists(sourceFile))
            {
                var sourceChecksum = ComputeSHA256Hash(sourceFile);
                var outputChecksum = ComputeSHA256Hash(outputFile);

                outputChecksum.Should().Be(sourceChecksum, 
                    "Output file should be identical copy of source file");
            }
        }

        [Fact]
        public void SupplementalFileProcessing_ShouldMatchLegacyScriptBehavior()
        {
            // Arrange & Act & Assert
            const string jobNumber = "69172";
            var expectedPath = _configuration.GetSupplementalTablePath(jobNumber);
            var expectedFileName = Path.GetFileName(expectedPath);

            // Validate file naming convention matches legacy script: $job.se1
            expectedFileName.Should().Be($"{jobNumber}.se1", 
                "File naming should match legacy script convention");

            // Validate file extension matches legacy script
            Path.GetExtension(expectedPath).Should().Be(".se1", 
                "File extension should match legacy script convention");

            // Validate the configuration matches legacy script parameters
            PipelineConfiguration.SupplementalTableFile.Should().Be("2503supptable.txt", 
                "Source file name should match legacy script");

            PipelineConfiguration.SupplementalExtension.Should().Be(".se1", 
                "Target file extension should match legacy script");
        }

        private static string ComputeSHA256Hash(string filePath)
        {
            using var sha256 = SHA256.Create();
            using var stream = File.OpenRead(filePath);
            var hashBytes = sha256.ComputeHash(stream);
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(_testOutputDirectory))
                {
                    Directory.Delete(_testOutputDirectory, true);
                }
            }
            catch (Exception ex)
            {
                _logger?.Warning(ex, "Failed to cleanup test directory: {Directory}", _testOutputDirectory);
            }
        }
    }
}
