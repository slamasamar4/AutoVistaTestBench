using AutoVistaTestBench.Core.Models;

namespace AutoVistaTestBench.Core.Interfaces
{
    /// <summary>
    /// Provides CAN bus message processing services.
    /// In production, this would interface with a physical CAN adapter
    /// (e.g., Vector VN1610, PEAK PCAN-USB, Kvaser Leaf).
    /// </summary>
    public interface ICanBusService
    {
        /// <summary>Total CAN frames received in the current session.</summary>
        long FrameCount { get; }

        /// <summary>Error frame count — high counts indicate bus electrical issues.</summary>
        long ErrorFrameCount { get; }

        /// <summary>Estimated bus load as a percentage (0.0 – 100.0).</summary>
        double BusLoadPercent { get; }

        /// <summary>
        /// Processes an incoming CAN frame, applying signal decoding and bus statistics.
        /// </summary>
        void ProcessFrame(CanFrame frame);

        /// <summary>Resets counters for a new session.</summary>
        void Reset();

        /// <summary>Raised when the bus load exceeds a configurable warning threshold.</summary>
        event EventHandler<double>? BusLoadWarning;
    }
}