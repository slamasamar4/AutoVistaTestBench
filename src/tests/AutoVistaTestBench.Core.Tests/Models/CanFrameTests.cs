using AutoVistaTestBench.Core.Models;
using FluentAssertions;
using Xunit;

namespace AutoVistaTestBench.Core.Tests.Models
{
    /// <summary>
    /// Unit tests for the CanFrame model.
    /// Tests frame construction, DLC enforcement, and signal decoding.
    /// </summary>
    public class CanFrameTests
    {
        [Fact]
        public void DataLengthCode_ShouldReturnCorrectDlc()
        {
            // Arrange
            var frame = new CanFrame
            {
                ArbitrationId = 0x0C0,
                Data = new byte[] { 0x12, 0x34, 0x56, 0x78, 0x00, 0x00, 0x00, 0x00 }
            };

            // Assert
            frame.DataLengthCode.Should().Be(8);
        }

        [Fact]
        public void DataLengthCode_ShouldCapAt8Bytes()
        {
            // Arrange — 10 bytes (invalid for standard CAN)
            var frame = new CanFrame
            {
                Data = new byte[10]
            };

            // Assert — DLC must not exceed 8
            frame.DataLengthCode.Should().Be(8);
        }

        [Theory]
        [InlineData(new byte[] { 0x1F, 0x40, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, 0, (ushort)0x1F40)]
        [InlineData(new byte[] { 0x00, 0xFF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, 0, (ushort)0x00FF)]
        [InlineData(new byte[] { 0x00, 0x00, 0xAB, 0xCD, 0x00, 0x00, 0x00, 0x00 }, 2, (ushort)0xABCD)]
        public void DecodeUInt16BigEndian_ShouldDecodeCorrectly(
            byte[] data, int startByte, ushort expected)
        {
            // Arrange
            var frame = new CanFrame { Data = data };

            // Act
            ushort result = frame.DecodeUInt16BigEndian(startByte);

            // Assert
            result.Should().Be(expected);
        }

        [Fact]
        public void DecodeUInt16BigEndian_WithInsufficientData_ShouldReturnZero()
        {
            // Arrange
            var frame = new CanFrame { Data = new byte[] { 0xFF } }; // Only 1 byte

            // Act
            ushort result = frame.DecodeUInt16BigEndian(0);

            // Assert
            result.Should().Be(0);
        }

        [Fact]
        public void ToString_ShouldContainArbitrationId()
        {
            // Arrange
            var frame = new CanFrame
            {
                ArbitrationId = 0x0C0,
                Data = new byte[] { 0x12, 0x34, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 },
                Timestamp = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc)
            };

            // Act
            string str = frame.ToString();

            // Assert
            str.Should().Contain("ID:0x0C0");
            str.Should().Contain("DLC:8");
            str.Should().Contain("12 34");
        }

        [Fact]
        public void EmptyFrame_ShouldHaveDlcOfZero()
        {
            // Arrange
            var frame = new CanFrame(); // Data = Array.Empty<byte>()

            // Assert
            frame.DataLengthCode.Should().Be(0);
        }
    }
}