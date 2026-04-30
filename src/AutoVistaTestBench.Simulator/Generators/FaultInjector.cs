using AutoVistaTestBench.Core.Models;

namespace AutoVistaTestBench.Simulator.Generators
{
    /// <summary>
    /// Injects controlled fault conditions into the simulation.
    /// Fault injection is a critical capability in automotive test benches —
    /// it verifies that ECU fault detection and diagnostic trouble code (DTC)
    /// logic responds correctly to abnormal inputs.
    /// 
    /// Supported fault types:
    /// - Open circuit (sensor disconnected → value drops to min)
    /// - Short to ground (value clamps to 0)
    /// - Short to supply (value clamps to max)
    /// - Value freeze (last valid value stuck)
    /// - Noise burst (high-frequency noise injection)
    /// </summary>
    public class FaultInjector
    {
        private readonly Random _random;
        private readonly Dictionary<string, FaultState> _activeFaults = new();

        public FaultInjector(int seed = 7)
        {
            _random = new Random(seed);
        }

        /// <summary>Returns true if a random fault should be triggered this cycle.</summary>
        public bool ShouldInjectFault(string channelId, double faultProbability = 0.003)
        {
            return !_activeFaults.ContainsKey(channelId) && _random.NextDouble() < faultProbability;
        }

        /// <summary>Registers a new active fault for a channel.</summary>
        public void InjectFault(string channelId, FaultType type, int durationSamples)
        {
            _activeFaults[channelId] = new FaultState
            {
                Type = type,
                RemainingDuration = durationSamples
            };
        }

        /// <summary>
        /// Applies the active fault transformation to a value.
        /// Returns the modified value and decrements the fault duration counter.
        /// </summary>
        public double ApplyFault(string channelId, double originalValue, double minValue, double maxValue)
        {
            if (!_activeFaults.TryGetValue(channelId, out var fault))
                return originalValue;

            fault.RemainingDuration--;
            if (fault.RemainingDuration <= 0)
            {
                _activeFaults.Remove(channelId);
                return originalValue;
            }

            return fault.Type switch
            {
                FaultType.OpenCircuit => minValue,
                FaultType.ShortToGround => 0.0,
                FaultType.ShortToSupply => maxValue,
                FaultType.ValueFreeze => fault.FrozenValue,
                FaultType.NoiseBurst => originalValue + (_random.NextDouble() - 0.5) * (maxValue - minValue) * 0.5,
                _ => originalValue
            };
        }

        /// <summary>Creates an AnomalyReport for an injected fault event.</summary>
        public AnomalyReport CreateAnomalyReport(SensorChannel channel, FaultType faultType)
        {
            return new AnomalyReport
            {
                ChannelId = channel.Id,
                ChannelName = channel.Name,
                TriggerValue = channel.CurrentValue,
                ThresholdValue = channel.FaultThreshold,
                Description = $"Injected fault: {faultType} on channel {channel.Id}. " +
                              $"Value: {channel.CurrentValue:F3} {channel.Unit}",
                DetectedAt = DateTime.UtcNow
            };
        }

        /// <summary>True if a channel currently has an active injected fault.</summary>
        public bool HasActiveFault(string channelId) => _activeFaults.ContainsKey(channelId);

        private class FaultState
        {
            public FaultType Type { get; set; }
            public int RemainingDuration { get; set; }
            public double FrozenValue { get; set; }
        }
    }

    public enum FaultType
    {
        OpenCircuit,
        ShortToGround,
        ShortToSupply,
        ValueFreeze,
        NoiseBurst
    }
}