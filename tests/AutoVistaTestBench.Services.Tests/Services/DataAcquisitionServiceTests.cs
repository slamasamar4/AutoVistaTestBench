using AutoVistaTestBench.Core.Enums;
using AutoVistaTestBench.Core.Interfaces;
using AutoVistaTestBench.Core.Models;
using AutoVistaTestBench.Services.Acquisition;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace AutoVistaTestBench.Services.Tests.Services
{
    /// <summary>
    /// Unit tests for DataAcquisitionService.
    /// Uses Moq to mock hardware simulator and logging service,
    /// allowing session lifecycle to be tested without real hardware.
    /// </summary>
    public class DataAcquisitionServiceTests : IDisposable
    {
        private readonly Mock<IHardwareSimulator> _simulatorMock;
        private readonly Mock<ILoggingService> _loggingMock;
        private readonly Mock<ICanBusService> _canBusMock;
        private readonly DataAcquisitionService _service;

        public DataAcquisitionServiceTests()
        {
            _simulatorMock = new Mock<IHardwareSimulator>();
            _loggingMock = new Mock<ILoggingService>();
            _canBusMock = new Mock<ICanBusService>();

            // Setup simulator mock
            _simulatorMock
                .Setup(s => s.Modules)
                .Returns(new List<EcuModule>().AsReadOnly());

            // Setup simulator to fire ChannelUpdated event
            _simulatorMock.SetupAdd(s => s.ChannelUpdated += It.IsAny<EventHandler<SensorChannel>>());
            _simulatorMock.SetupAdd(s => s.CanFrameReceived += It.IsAny<EventHandler<CanFrame>>());
            _simulatorMock.SetupAdd(s => s.AnomalyDetected += It.IsAny<EventHandler<AnomalyReport>>());

            _loggingMock
                .Setup(l => l.OpenSessionLogAsync(It.IsAny<TestSession>()))
                .Returns(Task.CompletedTask);
            _loggingMock
                .Setup(l => l.CloseSessionLogAsync())
                .Returns(Task.CompletedTask);
            _loggingMock
                .Setup(l => l.WriteAsync(
                    It.IsAny<LogSeverity>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string?>(), It.IsAny<double?>()))
                .Returns(Task.CompletedTask);

            _service = new DataAcquisitionService(
                _simulatorMock.Object,
                _loggingMock.Object,
                _canBusMock.Object,
                NullLogger<DataAcquisitionService>.Instance);
        }

        [Fact]
        public void InitialState_IsAcquiringShouldBeFalse()
        {
            _service.IsAcquiring.Should().BeFalse();
        }

        [Fact]
        public void InitialState_CurrentSessionShouldBeNull()
        {
            _service.CurrentSession.Should().BeNull();
        }

        [Fact]
        public async Task StartSessionAsync_ShouldSetIsAcquiringTrue()
        {
            // Act
            await _service.StartSessionAsync("TestSession", "Operator", "VIN-001");

            // Assert
            _service.IsAcquiring.Should().BeTrue();

            // Cleanup
            await _service.StopSessionAsync();
        }

        [Fact]
        public async Task StartSessionAsync_ShouldCreateCurrentSession()
        {
            // Act
            await _service.StartSessionAsync("TestSession", "Operator", "VIN-001");

            // Assert
            _service.CurrentSession.Should().NotBeNull();
            _service.CurrentSession!.SessionName.Should().Be("TestSession");
            _service.CurrentSession.OperatorName.Should().Be("Operator");
            _service.CurrentSession.VehicleId.Should().Be("VIN-001");

            // Cleanup
            await _service.StopSessionAsync();
        }

        [Fact]
        public async Task StartSessionAsync_ShouldStartSimulator()
        {
            // Act
            await _service.StartSessionAsync("TestSession", "Operator", "VIN-001");

            // Assert
            _simulatorMock.Verify(s => s.Start(), Times.Once);

            // Cleanup
            await _service.StopSessionAsync();
        }

        [Fact]
        public async Task StartSessionAsync_ShouldOpenLogFile()
        {
            // Act
            await _service.StartSessionAsync("TestSession", "Operator", "VIN-001");

            // Assert
            _loggingMock.Verify(
                l => l.OpenSessionLogAsync(It.IsAny<TestSession>()),
                Times.Once);

            // Cleanup
            await _service.StopSessionAsync();
        }

        [Fact]
        public async Task StartSessionAsync_WhenAlreadyRunning_ShouldThrowInvalidOperationException()
        {
            // Arrange
            await _service.StartSessionAsync("Session1", "Op", "VIN");

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.StartSessionAsync("Session2", "Op", "VIN"));

            // Cleanup
            await _service.StopSessionAsync();
        }

        [Fact]
        public async Task StopSessionAsync_ShouldSetIsAcquiringFalse()
        {
            // Arrange
            await _service.StartSessionAsync("TestSession", "Operator", "VIN-001");

            // Act
            await _service.StopSessionAsync();

            // Assert
            _service.IsAcquiring.Should().BeFalse();
        }

        [Fact]
        public async Task StopSessionAsync_ShouldStopSimulator()
        {
            // Arrange
            await _service.StartSessionAsync("TestSession", "Operator", "VIN-001");

            // Act
            await _service.StopSessionAsync();

            // Assert
            _simulatorMock.Verify(s => s.Stop(), Times.Once);
        }

        [Fact]
        public async Task StopSessionAsync_ShouldSetSessionEndTime()
        {
            // Arrange
            var before = DateTime.UtcNow;
            await _service.StartSessionAsync("TestSession", "Operator", "VIN-001");

            // Act
            await _service.StopSessionAsync();

            // Assert
            _service.CurrentSession!.EndTime.Should().NotBeNull();
            _service.CurrentSession.EndTime!.Value.Should().BeOnOrAfter(before);
        }

        [Fact]
        public async Task StopSessionAsync_WhenNotRunning_ShouldNotThrow()
        {
            // Act & Assert — should not throw
            var exception = await Record.ExceptionAsync(() => _service.StopSessionAsync());
            exception.Should().BeNull();
        }

        public void Dispose()
        {
            _service.Dispose();
        }
    }
}