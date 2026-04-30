using AutoVistaTestBench.Core.Enums;
using AutoVistaTestBench.Core.Models;
using AutoVistaTestBench.Simulator.Generators;

namespace AutoVistaTestBench.Simulator
{
    /// <summary>
    /// Simulates a complete ECU module with multiple sensor channels.
    /// Each module runs its own update logic and can inject faults independently.
    /// This mirrors the architecture of real HIL (Hardware-in-the-Loop) test systems
    /// where each ECU module has its own acquisition context.
    /// </summary>
    public class EcuModuleSimulator
    {
        private readonly EcuModule _module;
        private readonly Dictionary<string, TemperatureSensorGenerator> _tempGenerators = new();
        private readonly Dictionary<string, VoltageChannelGenerator> _voltGenerators = new();
        private readonly FaultInjector _faultInjector;

        public EcuModule Module => _module;

        public EcuModuleSimulator(EcuModule module)
        {
            _module = module;
            _faultInjector = new FaultInjector(module.CanNodeAddress);

            // Initialize generators for each channel based on sensor type
            foreach (var channel in module.Channels)
            {
                switch (channel.Type)
                {
                    case SensorType.Temperature:
                        _tempGenerators[channel.Id] = new TemperatureSensorGenerator(
                            (channel.MinValue + channel.MaxValue) / 2,
                            channel.MinValue, channel.MaxValue,
                            seed: channel.Id.GetHashCode());
                        break;
                    case SensorType.Voltage:
                        _voltGenerators[channel.Id] = new VoltageChannelGenerator(
                            (channel.MinValue + channel.MaxValue) / 2,
                            seed: channel.Id.GetHashCode());
                        break;
                }
            }
        }

        /// <summary>
        /// Updates all channels with new simulated values.
        /// Returns channels that have changed status (for anomaly detection).
        /// </summary>
        public IEnumerable<(SensorChannel Channel, bool IsFault)> Tick()
        {
            _module.LastHeartbeat = DateTime.UtcNow;
            _module.IsConnected = true;

            foreach (var channel in _module.Channels)
            {
                if (channel.Status == ChannelStatus.Disabled)
                    continue;

                double rawValue = GenerateRawValue(channel);

                // Check if fault injection is needed (rare random fault simulation)
                if (_faultInjector.ShouldInjectFault(channel.Id, 0.002))
                {
                    var faultType = (FaultType)new Random().Next(0, 5);
                    _faultInjector.InjectFault(channel.Id, faultType, durationSamples: new Random().Next(3, 15));
                }

                // Apply any active fault transformations
                double finalValue = _faultInjector.ApplyFault(
                    channel.Id, rawValue, channel.MinValue, channel.MaxValue);

                var previousStatus = channel.Status;
                channel.UpdateValue(finalValue);

                bool becameFault = previousStatus != ChannelStatus.Fault
                                   && channel.Status == ChannelStatus.Fault;

                yield return (channel, becameFault);
            }

            // Update module-level status based on worst channel
            _module.Status = _module.Channels.Any(c => c.Status == ChannelStatus.Fault)
                ? ChannelStatus.Fault
                : _module.Channels.Any(c => c.Status == ChannelStatus.Warning)
                    ? ChannelStatus.Warning
                    : ChannelStatus.Active;
        }

        /// <summary>
        /// Generates a raw (pre-fault) value for a channel using the appropriate generator.
        /// RPM and Pressure channels use simple random-walk models for realism.
        /// </summary>
        private double GenerateRawValue(SensorChannel channel)
        {
            return channel.Type switch
            {
                SensorType.Temperature when _tempGenerators.ContainsKey(channel.Id)
                    => _tempGenerators[channel.Id].NextSample(),

                SensorType.Voltage when _voltGenerators.ContainsKey(channel.Id)
                    => _voltGenerators[channel.Id].NextSample(),

                SensorType.Speed
                    => Math.Clamp(channel.CurrentValue + (new Random().NextDouble() - 0.48) * 200,
                        channel.MinValue, channel.MaxValue),

                SensorType.Pressure
                    => Math.Clamp(channel.CurrentValue + (new Random().NextDouble() - 0.49) * 0.5,
                        channel.MinValue, channel.MaxValue),

                SensorType.Current
                    => Math.Clamp(channel.CurrentValue + (new Random().NextDouble() - 0.49) * 1.0,
                        channel.MinValue, channel.MaxValue),

                _ => channel.CurrentValue
            };
        }
    }
}