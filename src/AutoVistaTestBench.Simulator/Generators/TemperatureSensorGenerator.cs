namespace AutoVistaTestBench.Simulator.Generators
{
    /// <summary>
    /// Generates realistic temperature sensor readings using a combination of:
    /// - A baseline drift (simulating warm-up over time)
    /// - Gaussian noise (simulating ADC quantization and sensor noise)
    /// - Occasional spikes (simulating real-world transients)
    /// 
    /// This mirrors the behavior of NTC thermistor or K-type thermocouple inputs
    /// on automotive test benches.
    /// </summary>
    public class TemperatureSensorGenerator
    {
        private readonly Random _random;
        private double _currentTemp;
        private double _targetTemp;
        private readonly double _minTemp;
        private readonly double _maxTemp;
        private int _sampleCount;

        // Noise standard deviation in degrees Celsius — typical for automotive NTC sensors
        private const double NoiseStdDev = 0.3;

        // Probability of a transient spike per sample
        private const double SpikeProbability = 0.02;

        public TemperatureSensorGenerator(double baseTemp, double minTemp, double maxTemp, int seed = 42)
        {
            _random = new Random(seed);
            _currentTemp = baseTemp;
            _targetTemp = baseTemp;
            _minTemp = minTemp;
            _maxTemp = maxTemp;
        }

        /// <summary>
        /// Generates the next temperature sample.
        /// The value slowly drifts toward a randomly changing target,
        /// with Gaussian noise and occasional spikes.
        /// </summary>
        public double NextSample()
        {
            _sampleCount++;

            // Every ~10 seconds, pick a new target temperature (simulates load changes)
            if (_sampleCount % 10 == 0)
            {
                _targetTemp = _minTemp + _random.NextDouble() * (_maxTemp - _minTemp);
            }

            // Slowly drift toward target (RC filter behavior, tau ≈ 5 samples)
            _currentTemp += (_targetTemp - _currentTemp) * 0.2;

            // Add Gaussian noise using Box-Muller transform
            double noise = BoxMullerNoise(NoiseStdDev);
            double value = _currentTemp + noise;

            // Occasional spike (simulates electrical interference or real transient)
            if (_random.NextDouble() < SpikeProbability)
            {
                value += (_random.NextDouble() > 0.5 ? 1 : -1) * _random.NextDouble() * 8.0;
            }

            return Math.Clamp(value, _minTemp - 10, _maxTemp + 10);
        }

        /// <summary>
        /// Generates Gaussian-distributed noise using the Box-Muller transform.
        /// This produces more realistic sensor noise than uniform random noise.
        /// </summary>
        private double BoxMullerNoise(double stdDev)
        {
            double u1 = 1.0 - _random.NextDouble();
            double u2 = 1.0 - _random.NextDouble();
            double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
            return stdDev * randStdNormal;
        }
    }
}