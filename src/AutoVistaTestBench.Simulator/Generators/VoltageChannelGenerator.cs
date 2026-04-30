namespace AutoVistaTestBench.Simulator.Generators
{
    /// <summary>
    /// Generates simulated voltage channel readings for 12V automotive electrical systems.
    /// Simulates:
    /// - Normal operating range (13.8V–14.4V when charging)
    /// - Load dumps (voltage spikes)
    /// - Battery discharge (voltage drop)
    /// - Alternator ripple (AC component on DC bus)
    /// </summary>
    public class VoltageChannelGenerator
    {
        private readonly Random _random;
        private double _baseVoltage;
        private int _sampleCount;
        private bool _inDischargeMode;
        private double _dischargeRate;

        public VoltageChannelGenerator(double baseVoltage = 13.8, int seed = 137)
        {
            _random = new Random(seed);
            _baseVoltage = baseVoltage;
        }

        /// <summary>
        /// Generates the next voltage sample with realistic automotive characteristics.
        /// </summary>
        public double NextSample()
        {
            _sampleCount++;

            // Simulate periodic discharge/charge cycles
            if (_sampleCount % 50 == 0)
            {
                _inDischargeMode = _random.NextDouble() < 0.3;
                _dischargeRate = _random.NextDouble() * 0.05;
            }

            if (_inDischargeMode)
                _baseVoltage = Math.Max(11.5, _baseVoltage - _dischargeRate);
            else
                _baseVoltage = Math.Min(14.4, _baseVoltage + _dischargeRate * 0.5);

            // Alternator ripple simulation (120Hz component ≈ 100mV peak-to-peak)
            double ripple = 0.05 * Math.Sin(2 * Math.PI * _sampleCount / 8.0);

            // Gaussian noise (ADC resolution simulation, ~10mV noise floor)
            double u1 = 1.0 - _random.NextDouble();
            double u2 = 1.0 - _random.NextDouble();
            double noise = 0.015 * Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);

            // Load dump spike (rare, 0.5% chance per sample)
            double spike = 0.0;
            if (_random.NextDouble() < 0.005)
                spike = 1.5 + _random.NextDouble() * 3.0; // 1.5V–4.5V spike

            return _baseVoltage + ripple + noise + spike;
        }
    }
}