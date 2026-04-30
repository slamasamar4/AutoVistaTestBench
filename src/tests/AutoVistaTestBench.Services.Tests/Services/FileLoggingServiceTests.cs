using AutoVistaTestBench.Core.Enums;
using AutoVistaTestBench.Core.Models;
using AutoVistaTestBench.Services.Logging;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AutoVistaTestBench.Services.Tests.Services
{
    /// <summary>
    /// Integration tests for FileLoggingService.
    /// Tests actual file creation and log entry persistence.
    /// Uses a temporary test directory, cleaned up after each test.
    /// </summary>
    public class FileLoggingServiceTests : IDisposable
    {
        private readonly FileLoggingService _service;
        private readonly string _testLogDir;

        public FileLoggingServiceTests()
        {
            _service = new FileLoggingService(NullLogger<FileLoggingService>.Instance);

            // Override log directory for testing (use temp folder)
            _testLogDir = Path.Combine(Path.GetTempPath(), "AutoVistaTests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testLogDir);
        }

        [Fact]
        public async Task OpenSessionLog_ShouldSetIsOpenToTrue()
        {
            // Arrange
            var session = CreateTestSession();

            // Act
            await _service.OpenSessionLogAsync(session);

            // Assert
            _service.IsOpen.Should().BeTrue();

            // Cleanup
            await _service.CloseSessionLogAsync();
        }

        [Fact]
        public async Task CloseSessionLog_ShouldSetIsOpenToFalse()
        {
            // Arrange
            var session = CreateTestSession();
            await _service.OpenSessionLogAsync(session);

            // Act
            await _service.CloseSessionLogAsync();

            // Assert
            _service.IsOpen.Should().BeFalse();
        }

        [Fact]
        public async Task WriteAsync_ShouldAddEntryToSessionEntries()
        {
            // Arrange
            var session = CreateTestSession();
            await _service.OpenSessionLogAsync(session);

            // Act
            await _service.WriteAsync(LogSeverity.Info, "TestSource",
                "Test message", "CH_01", 42.5);

            // Give the writer thread time to process
            await Task.Delay(200);

            // Assert
            var entries = _service.GetSessionEntries();
            entries.Should().HaveCount(1);
            entries[0].Severity.Should().Be(LogSeverity.Info);
            entries[0].Source.Should().Be("TestSource");
            entries[0].Message.Should().Be("Test message");
            entries[0].ChannelId.Should().Be("CH_01");
            entries[0].Value.Should().BeApproximately(42.5, 0.001);

            // Cleanup
            await _service.CloseSessionLogAsync();
        }

        [Fact]
        public async Task WriteAsync_MultipleEntries_ShouldAllBeRecorded()
        {
            // Arrange
            var session = CreateTestSession();
            await _service.OpenSessionLogAsync(session);

            // Act — write 10 entries rapidly
            for (int i = 0; i < 10; i++)
            {
                await _service.WriteAsync(LogSeverity.Debug, "BatchTest",
                    $"Entry {i}", value: (double)i);
            }

            await Task.Delay(300); // Allow writer thread to process

            // Assert
            var entries = _service.GetSessionEntries();
            entries.Should().HaveCount(10);

            // Cleanup
            await _service.CloseSessionLogAsync();
        }

        [Fact]
        public async Task GetSessionEntries_WithSeverities_ShouldReturnAllSeverities()
        {
            // Arrange
            var session = CreateTestSession();
            await _service.OpenSessionLogAsync(session);

            // Act — write one entry of each severity
            foreach (var severity in Enum.GetValues<LogSeverity>())
            {
                await _service.WriteAsync(severity, "SeverityTest", $"Test {severity}");
            }

            await Task.Delay(300);

            // Assert — all 5 severities should be present
            var entries = _service.GetSessionEntries();
            entries.Should().HaveCount(Enum.GetValues<LogSeverity>().Length);
            entries.Select(e => e.Severity).Distinct().Should()
                .HaveCount(Enum.GetValues<LogSeverity>().Length);

            // Cleanup
            await _service.CloseSessionLogAsync();
        }

        [Fact]
        public async Task OpenSessionLog_ShouldCreateLogFile()
        {
            // Arrange
            var session = CreateTestSession();

            // Act
            await _service.OpenSessionLogAsync(session);
            await _service.WriteAsync(LogSeverity.Info, "Test", "File creation test");
            await Task.Delay(200);
            await _service.CloseSessionLogAsync();

            // Assert — log file should exist
            File.Exists(session.LogFilePath).Should().BeTrue();
        }

        [Fact]
        public async Task LogFile_ShouldContainSessionHeader()
        {
            // Arrange
            var session = CreateTestSession("MyTestSession", "JohnDoe");

            // Act
            await _service.OpenSessionLogAsync(session);
            await _service.CloseSessionLogAsync();

            // Assert — file header should contain session name
            var content = await File.ReadAllTextAsync(session.LogFilePath);
            content.Should().Contain("MyTestSession");
            content.Should().Contain("JohnDoe");
        }

        private static TestSession CreateTestSession(
            string name = "TestSession",
            string operatorName = "TestOperator")
        {
            return new TestSession
            {
                SessionName = name,
                OperatorName = operatorName,
                VehicleId = "TEST-VIN-001",
                StartTime = DateTime.UtcNow,
                LogFilePath = Path.Combine(
                    Path.GetTempPath(), "AutoVistaTests",
                    $"test_{Guid.NewGuid():N}.log")
            };
        }

        public void Dispose()
        {
            // Cleanup: close any open log and remove temp test directory
            if (_service.IsOpen)
                _service.CloseSessionLogAsync().GetAwaiter().GetResult();

            _service.Dispose();

            if (Directory.Exists(_testLogDir))
            {
                try { Directory.Delete(_testLogDir, recursive: true); }
                catch { /* Ignore cleanup failures */ }
            }
        }
    }
}