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

        /// <summary>
        /// Adds a sensor channel to this ECU module.
        /// </summary>
        /// <param name="channel">The sensor channel to add.</param>
        public void AddChannel(SensorChannel channel)
        {
            if (!Channels.Any(c => c.Id == channel.Id))
            {
                Channels.Add(channel);
            }
        }

        /// <summary>
        /// Adds multiple sensor channels to this ECU module.
        /// </summary>
        /// <param name="channels">The sensor channels to add.</param>
        public void AddChannels(IEnumerable<SensorChannel> channels)
        {
            foreach (var channel in channels)
            {
                AddChannel(channel);
            }
        }

        /// <summary>
        /// Removes a sensor channel by ID.
        /// </summary>
        /// <param name="channelId">The ID of the channel to remove.</param>
        /// <returns>True if the channel was removed; otherwise, false.</returns>
        public bool RemoveChannel(string channelId)
        {
            var channel = GetChannel(channelId);
            return channel != null && Channels.Remove(channel);
        }

        /// <summary>
        /// Gets a sensor channel by ID.
        /// </summary>
        /// <param name="channelId">The ID of the channel to retrieve.</param>
        /// <returns>The sensor channel if found; otherwise, null.</returns>
        public SensorChannel? GetChannel(string channelId)
        {
            return Channels.FirstOrDefault(c => c.Id == channelId);
        }

        /// <summary>
        /// Gets a sensor channel by name.
        /// </summary>
        /// <param name="channelName">The name of the channel to retrieve.</param>
        /// <returns>The sensor channel if found; otherwise, null.</returns>
        public SensorChannel? GetChannelByName(string channelName)
        {
            return Channels.FirstOrDefault(c => c.Name.Equals(channelName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Updates the value of a specific sensor channel.
        /// </summary>
        /// <param name="channelId">The ID of the channel to update.</param>
        /// <param name="value">The new value to set.</param>
        /// <returns>True if the channel was updated; otherwise, false.</returns>
        public bool UpdateChannelValue(string channelId, double value)
        {
            var channel = GetChannel(channelId);
            if (channel == null) return false;
            
            channel.CurrentValue = value;
            channel.LastUpdateTime = DateTime.UtcNow;
            channel.Status = ChannelStatus.Active;
            
            // Check if value is valid, otherwise increment error count
            if (!channel.IsValueInRange())
            {
                ErrorCount++;
            }
            
            return true;
        }

        /// <summary>
        /// Updates the status of a specific sensor channel.
        /// </summary>
        /// <param name="channelId">The ID of the channel to update.</param>
        /// <param name="status">The new status to set.</param>
        /// <returns>True if the channel was updated; otherwise, false.</returns>
        public bool UpdateChannelStatus(string channelId, ChannelStatus status)
        {
            var channel = GetChannel(channelId);
            if (channel == null) return false;
            
            channel.Status = status;
            return true;
        }

        /// <summary>
        /// Gets all channels that are currently in alarm state.
        /// </summary>
        /// <returns>List of channels in alarm.</returns>
        public List<SensorChannel> GetChannelsInAlarm()
        {
            return Channels.Where(c => c.IsInAlarm).ToList();
        }

        /// <summary>
        /// Gets all channels that are currently in warning state.
        /// </summary>
        /// <returns>List of channels in warning.</returns>
        public List<SensorChannel> GetChannelsInWarning()
        {
            return Channels.Where(c => c.IsInWarning && !c.IsInAlarm).ToList();
        }

        /// <summary>
        /// Gets all channels that are in normal operating state.
        /// </summary>
        /// <returns>List of channels operating normally.</returns>
        public List<SensorChannel> GetNormalChannels()
        {
            return Channels.Where(c => !c.IsInAlarm && !c.IsInWarning).ToList();
        }

        /// <summary>
        /// Checks if any channel is currently in alarm state.
        /// </summary>
        /// <returns>True if at least one channel is in alarm; otherwise, false.</returns>
        public bool HasAnyAlarm()
        {
            return Channels.Any(c => c.IsInAlarm);
        }

        /// <summary>
        /// Checks if any channel is currently in warning state.
        /// </summary>
        /// <returns>True if at least one channel is in warning; otherwise, false.</returns>
        public bool HasAnyWarning()
        {
            return Channels.Any(c => c.IsInWarning);
        }

        /// <summary>
        /// Returns a detailed report of all channel statuses.
        /// </summary>
        /// <returns>Formatted string with channel details.</returns>
        public string GetChannelStatusReport()
        {
            var report = $"Channel Status Report for {Name} ({Id}):\n";
            report += $"Total Channels: {Channels.Count}\n";
            report += $"In Alarm: {GetChannelsInAlarm().Count}\n";
            report += $"In Warning: {GetChannelsInWarning().Count}\n";
            report += $"Normal: {GetNormalChannels().Count}\n";
            report += "\nDetails:\n";
            
            foreach (var channel in Channels)
            {
                report += $"  - {channel}\n";
            }
            
            return report;
        }

        /// <summary>
        /// Resets the error count for this ECU module.
        /// </summary>
        public void ResetErrorCount()
        {
            ErrorCount = 0;
        }

        /// <summary>
        /// Updates the heartbeat timestamp and connection status.
        /// </summary>
        public void UpdateHeartbeat()
        {
            LastHeartbeat = DateTime.UtcNow;
            IsConnected = true;
            Status = ChannelStatus.Active;
        }

        /// <summary>
        /// Marks the ECU module as disconnected.
        /// </summary>
        public void Disconnect()
        {
            IsConnected = false;
            Status = ChannelStatus.Fault;
        }
    }
}