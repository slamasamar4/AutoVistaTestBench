using AutoVistaTestBench.Core.Enums;

namespace AutoVistaTestBench.Core.Models
{
    /// <summary>
    /// Represents a simulated ECU (Electronic Control Unit) hardware module.
    /// In a real test bench, this would correspond to a physical device connected via
    /// USB, PCIe, or Ethernet (e.g., NI DAQ, Vector CANalyzer, dSPACE MicroLabBox).
    /// </summary>
    public class EcuModule
    {
        /// <summary>Unique module identifier (e.g., "ECU_POWERTRAIN_01").</summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>Descriptive module name (e.g., "Powertrain Control Module").</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Firmware version string reported by the module.</summary>
        public string FirmwareVersion { get; set; } = "1.0.0";

        /// <summary>Hardware serial number of the physical device.</summary>
        public string SerialNumber { get; set; } = string.Empty;

        /// <summary>Overall status of the ECU module.</summary>
        public ChannelStatus Status { get; set; } = ChannelStatus.Idle;

        /// <summary>List of sensor channels associated with this ECU module.</summary>
        public List<SensorChannel> Channels { get; set; } = new();

        /// <summary>True if the module is currently connected and communicating.</summary>
        public bool IsConnected { get; set; }

        /// <summary>Timestamp when the module was last seen communicating.</summary>
        public DateTime LastHeartbeat { get; set; }

        /// <summary>Cumulative error count since session start.</summary>
        public int ErrorCount { get; set; }

        /// <summary>CAN bus node address (0x00 – 0x7F for standard CAN).</summary>
        public byte CanNodeAddress { get; set; }

        /// <summary>
        /// Returns a summary string for logging and diagnostics.
        /// </summary>
        public override string ToString() =>
            $"[{Id}] {Name} FW:{FirmwareVersion} SN:{SerialNumber} Status:{Status} Errors:{ErrorCount}";
    }
}