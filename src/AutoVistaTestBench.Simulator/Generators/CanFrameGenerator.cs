using AutoVistaTestBench.Core.Models;

namespace AutoVistaTestBench.Simulator.Generators
{
    /// <summary>
    /// Generates simulated CAN bus frames mimicking typical automotive ECU messages.
    /// 
    /// Standard CAN IDs used here are loosely based on common OBD-II and 
    /// proprietary powertrain message layouts:
    /// - 0x0C0: Engine speed (RPM)
    /// - 0x0D0: Vehicle speed
    /// - 0x110: Throttle position
    /// - 0x180: Coolant temperature
    /// - 0x200: Battery status
    /// - 0x300: Diagnostic response
    /// </summary>
    public class CanFrameGenerator
    {
        private readonly Random _random;
        private double _engineRpm = 800.0;
        private double _vehicleSpeed = 0.0;
        private double _throttle = 0.0;
        private int _frameIndex;

        private static readonly uint[] MessageIds = { 0x0C0, 0x0D0, 0x110, 0x180, 0x200, 0x300 };

        public CanFrameGenerator(int seed = 99)
        {
            _random = new Random(seed);
        }

        /// <summary>
        /// Generates a batch of CAN frames for one simulation tick.
        /// Returns multiple frames to simulate realistic bus traffic density.
        /// </summary>
        public IEnumerable<CanFrame> GenerateFrames(string sourceModuleId)
        {
            _frameIndex++;

            // Update signal states
            _engineRpm = Math.Clamp(_engineRpm + (_random.NextDouble() - 0.48) * 150, 700, 6500);
            _vehicleSpeed = Math.Clamp(_vehicleSpeed + (_random.NextDouble() - 0.49) * 5, 0, 200);
            _throttle = Math.Clamp(_throttle + (_random.NextDouble() - 0.48) * 3, 0, 100);

            // Engine RPM frame (0x0C0) — sent every tick
            yield return BuildRpmFrame(sourceModuleId);

            // Vehicle speed frame (0x0D0) — sent every tick
            yield return BuildSpeedFrame(sourceModuleId);

            // Throttle position (0x110) — sent every 2 ticks
            if (_frameIndex % 2 == 0)
                yield return BuildThrottleFrame(sourceModuleId);

            // Battery status (0x200) — sent every 5 ticks
            if (_frameIndex % 5 == 0)
                yield return BuildBatteryFrame(sourceModuleId);

            // Occasional error frame simulation (1% chance)
            if (_random.NextDouble() < 0.01)
                yield return BuildErrorFrame(sourceModuleId);
        }

        private CanFrame BuildRpmFrame(string source)
        {
            // RPM is encoded as: raw_value = RPM / 0.25 → 16-bit big-endian
            ushort rpmRaw = (ushort)(_engineRpm / 0.25);
            return new CanFrame
            {
                ArbitrationId = 0x0C0,
                Data = new byte[] { (byte)(rpmRaw >> 8), (byte)(rpmRaw & 0xFF), 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 },
                SourceModuleId = source,
                Timestamp = DateTime.UtcNow
            };
        }

        private CanFrame BuildSpeedFrame(string source)
        {
            // Speed encoded as: raw = km/h * 100 → 16-bit big-endian
            ushort speedRaw = (ushort)(_vehicleSpeed * 100);
            return new CanFrame
            {
                ArbitrationId = 0x0D0,
                Data = new byte[] { (byte)(speedRaw >> 8), (byte)(speedRaw & 0xFF), 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 },
                SourceModuleId = source,
                Timestamp = DateTime.UtcNow
            };
        }

        private CanFrame BuildThrottleFrame(string source)
        {
            // Throttle: 0–100% → byte 0 (0x00–0x64)
            return new CanFrame
            {
                ArbitrationId = 0x110,
                Data = new byte[] { (byte)_throttle, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 },
                SourceModuleId = source,
                Timestamp = DateTime.UtcNow
            };
        }

        private CanFrame BuildBatteryFrame(string source)
        {
            // Battery voltage: raw = V * 10 → byte 0/1
            ushort voltRaw = (ushort)(138 + _random.Next(-5, 10)); // ~13.8V
            return new CanFrame
            {
                ArbitrationId = 0x200,
                Data = new byte[] { (byte)(voltRaw >> 8), (byte)(voltRaw & 0xFF), 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 },
                SourceModuleId = source,
                Timestamp = DateTime.UtcNow
            };
        }

        private CanFrame BuildErrorFrame(string source)
        {
            return new CanFrame
            {
                ArbitrationId = 0x7FF,
                Data = new byte[] { 0xFF, 0xFF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 },
                IsErrorFrame = true,
                SourceModuleId = source,
                Timestamp = DateTime.UtcNow
            };
        }
    }
}