using AutoVistaTestBench.Core.Models;

namespace AutoVistaTestBench.Core.Interfaces
{
    /// <summary>
    /// Defines the contract for a hardware simulator that mimics real test bench hardware.
    /// In production, this interface would be implemented by actual hardware drivers
    /// (e.g., NI-DAQmx, Vector CANlib, dSPACE SCALEXIO API).
    /// </summary>
    public interface IHardwareSimulator
    {
        /// <summary>All ECU modules managed by this simulator.</summary>
        IReadOnlyList<EcuModule> Modules { get; }

        /// <summary>True if the simulator is currently running.</summary>
        bool IsRunning { get; }

        /// <summary>
        /// Starts the hardware simulation.
        /// In real hardware: initializes driver, configures channels, begins DMA acquisition.
        /// </summary>
        void Start();

        /// <summary>
        /// Stops the hardware simulation and releases resources.
        /// In real hardware: stops acquisition, resets channels, closes device handle.
        /// </summary>
        void Stop();

        /// <summary>
        /// Raised whenever any channel value is updated by the simulator.
        /// Subscribers receive the updated channel for immediate UI refresh.
        /// </summary>
        event EventHandler<SensorChannel>? ChannelUpdated;

        /// <summary>
        /// Raised whenever a new CAN frame is generated/received.
        /// </summary>
        event EventHandler<CanFrame>? CanFrameReceived;

        /// <summary>
        /// Raised when the simulator detects or injects a fault condition.
        /// </summary>
        event EventHandler<AnomalyReport>? AnomalyDetected;
    }
}