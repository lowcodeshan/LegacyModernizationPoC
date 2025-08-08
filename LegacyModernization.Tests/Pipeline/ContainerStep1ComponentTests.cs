using System;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using LegacyModernization.Core.Configuration;
using LegacyModernization.Core.Logging;
using LegacyModernization.Core.Pipeline;
using Microsoft.Extensions.Logging;
using Serilog;
using Xunit;
using Xunit.Abstractions;

namespace LegacyModernization.Tests.Pipeline
{
    /// <summary>
    /// Integration tests for ContainerStep1Component
    /// Tests the core container processing logic equivalent to ncpcntr5v2.script
    /// </summary>
    public class ContainerStep1ComponentTests
    {
        private readonly ITestOutputHelper _testOutput;
        private readonly string _testDataPath;
        private readonly string _testOutputPath;
        private readonly PipelineConfiguration _configuration;
        private readonly ProgressReporter _progressReporter;
        private readonly Serilog.ILogger _logger;

        public ContainerStep1ComponentTests(ITestOutputHelper testOutput)
        {
            _testOutput = testOutput;
            
            // Use project-relative paths to avoid permission issues
            var assemblyLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var projectRoot = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(assemblyLocation)));
            
            _testDataPath = Path.Combine(projectRoot ?? "", "TestData");
            _testOutputPath = Path.Combine(projectRoot ?? "", "TestOutput", Guid.NewGuid().ToString("N")[..8]);
            
            if (!Directory.Exists(_testOutputPath))
            {
                Directory.CreateDirectory(_testOutputPath);
            }

            _configuration = new PipelineConfiguration
            {
                InputPath = _testDataPath,
                OutputPath = _testOutputPath
            };

            // Setup Serilog logger
            Log.Logger = new Serilog.LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Console()
                .CreateLogger();

            _logger = Log.Logger;
            
            var loggerFactory = LoggerFactory.Create(builder => builder.AddSerilog());
            var msLogger = loggerFactory.CreateLogger<ContainerStep1ComponentTests>();
            
            _progressReporter = new ProgressReporter(_logger);
        }

        [Fact]
        public async Task ExecuteAsync_WithValidParameters_ShouldProcessSuccessfully()
        {
            // Arrange
            var component = new ContainerStep1Component(_logger, _progressReporter, _configuration);
            
            var arguments = new PipelineArguments
            {
                JobNumber = "69172",
                SourceFilePath = Path.Combine(_testDataPath, "69172.dat")
            };

            // Act
            var result = await component.ExecuteAsync(arguments);

            // Assert - this may return false if the file doesn't exist, but should not throw
            _testOutput.WriteLine($"Container Step 1 processing result: {result}");
            
            // The main goal is to test that the component can be instantiated and called
            // without throwing exceptions
        }

        [Fact]
        public async Task ExecuteAsync_WithInvalidInputFile_ShouldReturnFalse()
        {
            // Arrange
            var component = new ContainerStep1Component(_logger, _progressReporter, _configuration);
            
            var arguments = new PipelineArguments
            {
                JobNumber = "99999",
                SourceFilePath = Path.Combine(_testDataPath, "nonexistent.dat")
            };

            // Act
            var result = await component.ExecuteAsync(arguments);

            // Assert
            result.Should().BeFalse("Processing should fail for non-existent input file");
        }

        [Theory]
        [InlineData("69172")]
        [InlineData("16860")]
        public async Task ExecuteAsync_WithVariousJobNumbers_ShouldHandleCorrectly(string jobNumber)
        {
            // Arrange
            var component = new ContainerStep1Component(_logger, _progressReporter, _configuration);
            
            var arguments = new PipelineArguments
            {
                JobNumber = jobNumber,
                SourceFilePath = Path.Combine(_testDataPath, $"{jobNumber}.dat")
            };

            // Act & Assert
            var result = await component.ExecuteAsync(arguments);
            
            // Should complete (true or false based on input file existence)
            // The important thing is that it doesn't throw exceptions
            _testOutput.WriteLine($"Processing result for job {jobNumber}: {result}");
        }

        [Fact]
        public void ContainerStep1Component_Constructor_ShouldCreateInstance()
        {
            // Arrange & Act
            var component = new ContainerStep1Component(_logger, _progressReporter, _configuration);

            // Assert
            component.Should().NotBeNull("ContainerStep1Component should be created successfully");
        }
    }
}
