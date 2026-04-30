using AutoVistaTestBench.Core.Interfaces;
using AutoVistaTestBench.Core.Models;
using Microsoft.Extensions.Logging;

namespace AutoVistaTestBench.Services.Communication
{
    /// <summary>
    /// Processes incoming CAN bus frames, maintains bus statistics,
    /// and raises warnings when bus load exceeds safe thresholds.
    /// 
    /// Bus load calculation:
    /// Standard CAN frame overhead = 47 bits (worst case, no stuffing)
    /// Data bits = DLC * 8
    /// At 500kbit/s, frame rate capacity ≈ 500000 / (47 + 64) ≈ 4500 frames/sec
    /// This simplified implementation tracks frames per second for bus load estimation.
    /// </summary>
    public class CanBusService : ICanBusService
    {
        private readonly ILogger<CanBusService> _logger;

        private long _frameCount;
        private long _errorFrameCount;
        private int _framesThisSecond;
        private DateTime _lastBusLoadCalc = DateTime.UtcNow;
        private double _busLoadPercent;

        // At 500kbit/s, safe operational limit is ~60% load (2700 frames/sec)
        private const double BusLoadWarningThreshold = 60.0;
        // Theoretical max frames/sec at 500kbit/s with 8-byte payload
        private const double MaxFramesPerSecond = 4500.0;

        public long FrameCount => _frameCount;
        public long ErrorFrameCount => _errorFrameCount;
        public double BusLoadPercent => _busLoadPercent;

        public event EventHandler<double>? BusLoadWarning;

        public CanBusService(ILogger<CanBusService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Processes a single CAN frame, updating all bus statistics.
        /// Thread-safe via Interlocked operations.
        /// </summary>
        public void ProcessFrame(CanFrame frame)
        {
            Interlocked.Increment(ref _frameCount);

            if (frame.IsErrorFrame)
            {
                Interlocked.Increment(ref _errorFrameCount);
                _logger.LogDebug("CAN error frame received: {Frame}", frame);
            }

            // Update bus load estimate every second
            Interlocked.Increment(ref _framesThisSecond);

            var now = DateTime.UtcNow;
            if ((now - _lastBusLoadCalc).TotalSeconds >= 1.0)
            {
                _busLoadPercent = (_framesThisSecond / MaxFramesPerSecond) * 100.0;
                Interlocked.Exchange(ref _framesThisSecond, 0);
                _lastBusLoadCalc = now;

                if (_busLoadPercent > BusLoadWarningThreshold)
                {
                    _logger.LogWarning("CAN bus load high: {Load:F1}%", _busLoadPercent);
                    BusLoadWarning?.Invoke(this, _busLoadPercent);
                }
            }
        }

        public void Reset()
        {
            Interlocked.Exchange(ref _frameCount, 0);
            Interlocked.Exchange(ref _errorFrameCount, 0);
            Interlocked.Exchange(ref _framesThisSecond, 0);
            _busLoadPercent = 0;
            _lastBusLoadCalc = DateTime.UtcNow;
            _logger.LogInformation("CAN bus service reset");
        }
    }
}