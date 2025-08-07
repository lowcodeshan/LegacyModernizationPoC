using Xunit;
using FluentAssertions;
using LegacyModernization.Core.Configuration;
using LegacyModernization.Core.Validation;
using System.IO;
using System.Threading.Tasks;
using Serilog;

namespace LegacyModernization.Tests
{
    public class PipelineConfigurationTests
    {
        [Fact]
        public void CreateDefault_ShouldCreateValidConfiguration()
        {
            // Arrange
            var baseDirectory = Path.Combine(Path.GetTempPath(), "LegacyModernizationTest");

            // Act
            var config = PipelineConfiguration.CreateDefault(baseDirectory);

            // Assert
            config.Should().NotBeNull();
            config.ProjectBase.Should().Be(baseDirectory);
            config.IsValid().Should().BeTrue();
            
            // Cleanup
            if (Directory.Exists(baseDirectory))
                Directory.Delete(baseDirectory, true);
        }

        [Fact]
        public void GetInputFilePath_ShouldReturnCorrectPath()
        {
            // Arrange
            var baseDirectory = Path.Combine(Path.GetTempPath(), "LegacyModernizationTest");
            var config = PipelineConfiguration.CreateDefault(baseDirectory);
            var jobNumber = "69172";

            // Act
            var inputPath = config.GetInputFilePath(jobNumber);

            // Assert
            inputPath.Should().EndWith("69172.dat");
            inputPath.Should().Contain(config.InputPath);
            
            // Cleanup
            if (Directory.Exists(baseDirectory))
                Directory.Delete(baseDirectory, true);
        }

        [Theory]
        [InlineData("69172", true)]
        [InlineData("12345", true)]
        [InlineData("abc", false)]
        [InlineData("", false)]
        [InlineData("-1", false)]
        public void PipelineArguments_IsValid_ShouldValidateJobNumber(string jobNumber, bool expectedValid)
        {
            // Arrange
            var arguments = new PipelineArguments
            {
                JobNumber = jobNumber
            };

            // Act
            var isValid = arguments.IsValid();

            // Assert
            isValid.Should().Be(expectedValid);
        }
    }

    public class FileComparisonUtilitiesTests
    {
        [Fact]
        public async Task AreFilesIdentical_WithIdenticalFiles_ShouldReturnTrue()
        {
            // Arrange
            var tempDir = Path.Combine(Path.GetTempPath(), "FileComparisonTest");
            Directory.CreateDirectory(tempDir);
            
            var file1 = Path.Combine(tempDir, "file1.txt");
            var file2 = Path.Combine(tempDir, "file2.txt");
            
            var testContent = "This is test content for file comparison.";
            await File.WriteAllTextAsync(file1, testContent);
            await File.WriteAllTextAsync(file2, testContent);

            // Act
            var result = await FileComparisonUtilities.AreFilesIdenticalAsync(file1, file2);

            // Assert
            result.Should().BeTrue();
            
            // Cleanup
            Directory.Delete(tempDir, true);
        }

        [Fact]
        public async Task AreFilesIdentical_WithDifferentFiles_ShouldReturnFalse()
        {
            // Arrange
            var tempDir = Path.Combine(Path.GetTempPath(), "FileComparisonTest");
            Directory.CreateDirectory(tempDir);
            
            var file1 = Path.Combine(tempDir, "file1.txt");
            var file2 = Path.Combine(tempDir, "file2.txt");
            
            await File.WriteAllTextAsync(file1, "Content 1");
            await File.WriteAllTextAsync(file2, "Content 2");

            // Act
            var result = await FileComparisonUtilities.AreFilesIdenticalAsync(file1, file2);

            // Assert
            result.Should().BeFalse();
            
            // Cleanup
            Directory.Delete(tempDir, true);
        }

        [Fact]
        public async Task GetDetailedComparison_ShouldProvideComprehensiveReport()
        {
            // Arrange
            var tempDir = Path.Combine(Path.GetTempPath(), "FileComparisonTest");
            Directory.CreateDirectory(tempDir);
            
            var file1 = Path.Combine(tempDir, "file1.txt");
            var file2 = Path.Combine(tempDir, "file2.txt");
            
            await File.WriteAllTextAsync(file1, "Hello World!");
            await File.WriteAllTextAsync(file2, "Hello Earth!");

            // Act
            var report = await FileComparisonUtilities.GetDetailedComparisonAsync(file1, file2);

            // Assert
            report.Should().NotBeNull();
            report.IsIdentical.Should().BeFalse();
            report.ExpectedFileSize.Should().Be(12);
            report.ActualFileSize.Should().Be(12);
            report.FirstDifferencePosition.Should().Be(6); // Position of 'W' vs 'E'
            
            // Cleanup
            Directory.Delete(tempDir, true);
        }
    }

    public class BaselineValidationTests
    {
        [Fact]
        public void ConstantsDefinedCorrectly()
        {
            // Assert that key constants match the legacy system
            PipelineConfiguration.ClientDept.Should().Be("250301");
            PipelineConfiguration.ServiceType.Should().Be("320");
            PipelineConfiguration.ContainerKey.Should().Be(1941);
            PipelineConfiguration.OptionLength.Should().Be(2000);
            PipelineConfiguration.Work2Length.Should().Be(4300);
            PipelineConfiguration.ProjectType.Should().Be("mblps");
        }
    }
}
