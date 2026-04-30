using AutoVistaTestBench.Core.Models;

namespace AutoVistaTestBench.Core.Interfaces
{
    /// <summary>
    /// Manages the lifecycle of a test session and coordinates data acquisition
    /// from hardware (real or simulated). Acts as the central orchestrator
    /// between the hardware simulator and the rest of the application.
    /// </summary>
    public interface IDataAcquisitionService
    {
        /// <summary>The currently active test session, or null if none is running.</summary>
        TestSession? CurrentSession { get; }

        /// <summary>True if a session is currently active and acquiring data.</summary>
        bool IsAcquiring { get; }

        /// <summary>All known ECU modules in the current configuration.</summary>
        IReadOnlyList<EcuModule> Modules { get; }

        /// <summary>
        /// Starts a new test session with the given name and operator.
        /// Initializes hardware, creates log file, and begins data acquisition.
        /// </summary>
        Task StartSessionAsync(string sessionName, string operatorName, string vehicleId);

        /// <summary>
        /// Stops the active session, finalizes the log file, and releases hardware.
        /// </summary>
        Task StopSessionAsync();

        /// <summary>Raised whenever a channel is updated with a new value.</summary>
        event EventHandler<SensorChannel>? ChannelUpdated;

        /// <summary>Raised whenever a new CAN frame is received.</summary>
        event EventHandler<CanFrame>? CanFrameReceived;

        /// <summary>Raised whenever an anomaly is detected.</summary>
        event EventHandler<AnomalyReport>? AnomalyDetected;

        /// <summary>Raised whenever a new log entry is created.</summary>
        event EventHandler<LogEntry>? LogEntryAdded;
    }
}