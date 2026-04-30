namespace AutoVistaTestBench.Core.Enums
{
    /// <summary>
    /// Represents the operational status of a sensor or I/O channel.
    /// Aligned with functional safety status patterns used in automotive test benches.
    /// </summary>
    public enum ChannelStatus
    {
        /// <summary>Channel is idle and not yet acquiring data.</summary>
        Idle,

        /// <summary>Channel is actively acquiring and reporting data.</summary>
        Active,

        /// <summary>Channel value is within warning threshold but not yet faulted.</summary>
        Warning,

        /// <summary>Channel has exceeded fault threshold or is in an error state.</summary>
        Fault,

        /// <summary>Channel is disabled or physically disconnected.</summary>
        Disabled
    }
}