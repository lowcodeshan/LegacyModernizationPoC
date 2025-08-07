using LegacyModernization.Core.Validation;
using LegacyModernization.Core.Configuration;
using FluentAssertions;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace LegacyModernization.Tests.Validation
{
    /// <summary>
    /// Tests for the baseline validation framework using known input/output pairs
    /// Testing with 69172.dat → 69172p.asc conversion
    /// </summary>
    public class BaselineValidationTests
    {
        private readonly string _testDataPath;
        private readonly string _expectedOutputPath;

        public BaselineValidationTests()
        {
            // Get the solution root directory
            var currentDir = Directory.GetCurrentDirectory();
            var solutionRoot = FindSolutionRoot(currentDir);
            
            _testDataPath = Path.Combine(solutionRoot, "TestData");
            _expectedOutputPath = Path.Combine(solutionRoot, "ExpectedOutput");
        }

        [Fact]
        public async Task FileComparisonUtilities_ShouldDetectIdenticalFiles()
        {
            // Arrange
            var testFile = Path.Combine(_testDataPath, "69172.dat");
            var tempFile = Path.Combine(_testDataPath, "temp_identical_test.dat");
            
            // Create a temporary identical file
            if (File.Exists(testFile))
            {
                File.Copy(testFile, tempFile, true);
            }
            else
            {
                // Create a small test file for validation
                await File.WriteAllTextAsync(testFile, "Test data for validation");
                await File.WriteAllTextAsync(tempFile, "Test data for validation");
            }

            try
            {
                // Act
                var areIdentical = await FileComparisonUtilities.AreFilesIdenticalAsync(testFile, tempFile);

                // Assert
                areIdentical.Should().BeTrue("Files with identical content should be detected as identical");
            }
            finally
            {
                // Cleanup
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }

        [Fact]
        public async Task FileComparisonUtilities_ShouldDetectDifferentFiles()
        {
            // Arrange
            var testFile1 = Path.Combine(_testDataPath, "test1.tmp");
            var testFile2 = Path.Combine(_testDataPath, "test2.tmp");
            
            await File.WriteAllTextAsync(testFile1, "File content A");
            await File.WriteAllTextAsync(testFile2, "File content B");

            try
            {
                // Act
                var areIdentical = await FileComparisonUtilities.AreFilesIdenticalAsync(testFile1, testFile2);

                // Assert
                areIdentical.Should().BeFalse("Files with different content should be detected as different");
            }
            finally
            {
                // Cleanup
                if (File.Exists(testFile1)) File.Delete(testFile1);
                if (File.Exists(testFile2)) File.Delete(testFile2);
            }
        }

        [Fact]
        public async Task GetDetailedComparison_ShouldProvideComprehensiveReport()
        {
            // Arrange
            var testFile1 = Path.Combine(_testDataPath, "report_test1.tmp");
            var testFile2 = Path.Combine(_testDataPath, "report_test2.tmp");
            
            await File.WriteAllTextAsync(testFile1, "ABCDEFGH");
            await File.WriteAllTextAsync(testFile2, "ABCDXFGH"); // Difference at position 4

            try
            {
                // Act
                var report = await FileComparisonUtilities.GetDetailedComparisonAsync(testFile1, testFile2);

                // Assert
                report.Should().NotBeNull();
                report.IsIdentical.Should().BeFalse();
                report.ExpectedFileSize.Should().Be(8);
                report.ActualFileSize.Should().Be(8);
                report.FirstDifferencePosition.Should().Be(4);
            }
            finally
            {
                // Cleanup
                if (File.Exists(testFile1)) File.Delete(testFile1);
                if (File.Exists(testFile2)) File.Delete(testFile2);
            }
        }

        [Fact]
        public void PipelineConfiguration_ShouldCreateValidDefaultConfiguration()
        {
            // Arrange & Act
            var solutionRoot = FindSolutionRoot(Directory.GetCurrentDirectory());
            var config = PipelineConfiguration.CreateDefault(solutionRoot);

            // Assert
            config.Should().NotBeNull();
            config.IsValid().Should().BeTrue();
            config.ProjectBase.Should().Be(solutionRoot);
            
            // Verify directories exist
            Directory.Exists(config.InputPath).Should().BeTrue();
            Directory.Exists(config.OutputPath).Should().BeTrue();
            Directory.Exists(config.LogPath).Should().BeTrue();
        }

        [Fact]
        public void PipelineConfiguration_ShouldGenerateCorrectFilePaths()
        {
            // Arrange
            var solutionRoot = FindSolutionRoot(Directory.GetCurrentDirectory());
            var config = PipelineConfiguration.CreateDefault(solutionRoot);
            var jobNumber = "69172";

            // Act
            var inputPath = config.GetInputFilePath(jobNumber);
            var paperBillPath = config.GetPaperBillOutputPath(jobNumber);
            var electronicBillPath = config.GetElectronicBillOutputPath(jobNumber);

            // Assert
            inputPath.Should().EndWith("69172.dat");
            paperBillPath.Should().EndWith("69172p.asc");
            electronicBillPath.Should().EndWith("69172e.txt");
        }

        [Fact]
        public void PipelineArguments_ShouldValidateCorrectJobNumber()
        {
            // Arrange
            var args = new PipelineArguments { JobNumber = "69172" };

            // Act
            var isValid = args.IsValid();

            // Assert
            isValid.Should().BeTrue();
        }

        [Fact]
        public void PipelineArguments_ShouldRejectInvalidJobNumber()
        {
            // Arrange
            var args = new PipelineArguments { JobNumber = "invalid" };

            // Act
            var isValid = args.IsValid();

            // Assert
            isValid.Should().BeFalse();
        }

        [Fact]
        public void PipelineArguments_GetUsage_ShouldReturnHelpText()
        {
            // Act
            var usage = PipelineArguments.GetUsage();

            // Assert
            usage.Should().NotBeNullOrEmpty();
            usage.Should().Contain("job-number");
            usage.Should().Contain("Usage:");
            usage.Should().Contain("Examples:");
        }

        private static string FindSolutionRoot(string startPath)
        {
            var current = new DirectoryInfo(startPath);
            
            while (current != null)
            {
                if (File.Exists(Path.Combine(current.FullName, "LegacyModernization.sln")) ||
                    Directory.Exists(Path.Combine(current.FullName, "LegacyModernization.Core")))
                {
                    return current.FullName;
                }
                current = current.Parent;
            }
            
            // Fallback for test environment
            return Path.Combine(startPath, "..", "..", "..", "..");
        }
    }
}
