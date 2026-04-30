namespace AutoVistaTestBench.Core.Models
{
    /// <summary>
    /// Represents a CAN (Controller Area Network) bus frame.
    /// CAN is the dominant communication protocol inside automotive ECUs.
    /// Standard CAN 2.0A frame with 11-bit identifier and up to 8 data bytes.
    /// </summary>
    public class CanFrame
    {
        /// <summary>11-bit CAN message identifier (0x000 – 0x7FF for standard frames).</summary>
        public uint ArbitrationId { get; set; }

        /// <summary>Data payload, maximum 8 bytes per CAN 2.0 specification.</summary>
        public byte[] Data { get; set; } = Array.Empty<byte>();

        /// <summary>Number of valid data bytes (DLC — Data Length Code, 0–8).</summary>
        public int DataLengthCode => Math.Min(Data.Length, 8);

        /// <summary>True if this is an extended 29-bit CAN ID frame.</summary>
        public bool IsExtendedId { get; set; }

        /// <summary>True if this is a Remote Transmission Request frame.</summary>
        public bool IsRemoteFrame { get; set; }

        /// <summary>True if an error was detected in this frame.</summary>
        public bool IsErrorFrame { get; set; }

        /// <summary>UTC timestamp when this frame was received/transmitted.</summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>Source ECU module ID for simulator traceability.</summary>
        public string SourceModuleId { get; set; } = string.Empty;

        /// <summary>
        /// Returns a formatted hex string representation of the CAN frame.
        /// Format: [Timestamp] ID:0x{ArbitrationId:X3} DLC:{DataLengthCode} Data:{hex bytes}
        /// This format is compatible with standard CAN log viewers (e.g., CANdb++, Vector CANalyzer).
        /// </summary>
        public override string ToString()
        {
            string dataHex = string.Join(" ", Data.Take(8).Select(b => b.ToString("X2")));
            return $"[{Timestamp:HH:mm:ss.fff}] ID:0x{ArbitrationId:X3} DLC:{DataLengthCode} Data:[{dataHex}]";
        }

        /// <summary>
        /// Decodes the first two bytes of the data payload as a big-endian uint16.
        /// Commonly used for physical signal decoding from CAN frames.
        /// </summary>
        public ushort DecodeUInt16BigEndian(int startByte = 0)
        {
            if (Data.Length < startByte + 2)
                return 0;
            return (ushort)((Data[startByte] << 8) | Data[startByte + 1]);
        }
    }
}