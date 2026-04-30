using AutoVistaTestBench.Core.Enums;
using AutoVistaTestBench.Core.Models;
using FluentAssertions;
using Xunit;

namespace AutoVistaTestBench.Core.Tests.Models
{
    /// <summary>
    /// Unit tests for the SensorChannel model.
    /// Tests threshold evaluation, status transitions, and value history behavior.
    /// </summary>
    public class SensorChannelTests
    {
        private static SensorChannel CreateTestChannel(
            double warningThreshold = 80.0,
            double faultThreshold = 100.0)
        {
            return new SensorChannel
            {
                Id = "TEST_CH_01",
                Name = "Test Temperature Channel",
                Type = SensorType.Temperature,
                Unit = "°C",
                MinValue = -40,
                MaxValue = 130,
                WarningThreshold = warningThreshold,
                FaultThreshold = faultThreshold,
                Status = ChannelStatus.Active
            };
        }

        [Fact]
        public void UpdateValue_WithNormalValue_ShouldSetStatusActive()
        {
            // Arrange
            var channel = CreateTestChannel();

            // Act
            channel.UpdateValue(50.0);

            // Assert
            channel.Status.Should().Be(ChannelStatus.Active);
            channel.CurrentValue.Should().BeApproximately(50.0, 0.001);
        }

        [Fact]
        public void UpdateValue_ExceedingWarningThreshold_ShouldSetStatusWarning()
        {
            // Arrange
            var channel = CreateTestChannel(warningThreshold: 80.0, faultThreshold: 100.0);

            // Act
            channel.UpdateValue(85.0);

            // Assert
            channel.Status.Should().Be(ChannelStatus.Warning,
                "value 85.0 exceeds warning threshold of 80.0");
        }

        [Fact]
        public void UpdateValue_ExceedingFaultThreshold_ShouldSetStatusFault()
        {
            // Arrange
            var channel = CreateTestChannel(warningThreshold: 80.0, faultThreshold: 100.0);

            // Act
            channel.UpdateValue(105.0);

            // Assert
            channel.Status.Should().Be(ChannelStatus.Fault,
                "value 105.0 exceeds fault threshold of 100.0");
        }

        [Fact]
        public void UpdateValue_ShouldMaintainValueHistory()
        {
            // Arrange
            var channel = CreateTestChannel();

            // Act — add 5 values
            for (int i = 0; i < 5; i++)
                channel.UpdateValue(i * 10.0);

            // Assert
            channel.ValueHistory.Should().HaveCount(5);
            channel.ValueHistory.Last().Should().BeApproximately(40.0, 0.001);
        }

        [Fact]
        public void UpdateValue_ShouldCapHistoryAt60Samples()
        {
            // Arrange
            var channel = CreateTestChannel();

            // Act — add 70 values
            for (int i = 0; i < 70; i++)
                channel.UpdateValue(i * 1.0);

            // Assert — queue should not exceed 60
            channel.ValueHistory.Should().HaveCount(60);
        }

        [Fact]
        public void UpdateValue_ShouldUpdateLastUpdatedTimestamp()
        {
            // Arrange
            var channel = CreateTestChannel();
            var before = DateTime.UtcNow;

            // Act
            channel.UpdateValue(42.0);

            // Assert
            channel.LastUpdated.Should().BeOnOrAfter(before);
            channel.LastUpdated.Should().BeOnOrBefore(DateTime.UtcNow);
        }

        [Theory]
        [InlineData(0.0, 0.0, 100.0, 0.0)]    // Min value → 0%
        [InlineData(100.0, 0.0, 100.0, 1.0)]  // Max value → 100%
        [InlineData(50.0, 0.0, 100.0, 0.5)]   // Mid value → 50%
        [InlineData(-20.0, -40.0, 60.0, 0.2)] // Negative range
        public void NormalizedValue_ShouldReturnCorrectNormalization(
            double value, double min, double max, double expected)
        {
            // Arrange
            var channel = CreateTestChannel();
            channel.MinValue = min;
            channel.MaxValue = max;
            channel.UpdateValue(value);

            // Assert
            channel.NormalizedValue.Should().BeApproximately(expected, 0.001);
        }

        [Fact]
        public void NormalizedValue_ShouldClampBetweenZeroAndOne()
        {
            // Arrange — value beyond max
            var channel = CreateTestChannel();
            channel.MinValue = 0;
            channel.MaxValue = 100;
            channel.UpdateValue(150.0); // Beyond max

            // Assert — NormalizedValue clamps to 1.0
            channel.NormalizedValue.Should().Be(1.0);
        }
    }
}