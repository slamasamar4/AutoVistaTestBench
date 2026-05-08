using AutoVistaTestBench.Core.Enums;
using System.Collections.Concurrent;

namespace AutoVistaTestBench.Core.Models
{
    /// <summary>
    /// Represents a sensor channel on an ECU module.
    /// </summary>
    public class SensorChannel
    {
        private readonly ConcurrentQueue<double> _valueHistory = new();
        private const int MaxHistorySize = 60;
        
        /// <summary>Unique channel identifier.</summary>
        public string Id { get; set; } = string.Empty;
        
        /// <summary>Channel name/description.</summary>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>Type of sensor (temperature, pressure, etc.).</summary>
        public SensorType Type { get; set; }
        
        /// <summary>Current value of the sensor.</summary>
        public double CurrentValue { get; set; }
        
        /// <summary>Unit of measurement (e.g., "°C", "kPa", "V").</summary>
        public string Unit { get; set; } = string.Empty;
        
        /// <summary>Status of this sensor channel.</summary>
        public ChannelStatus Status { get; set; } = ChannelStatus.Idle;
        
        /// <summary>Minimum valid value for this sensor.</summary>
        public double MinValue { get; set; }
        
        /// <summary>Maximum valid value for this sensor.</summary>
        public double MaxValue { get; set; }
        
        /// <summary>Warning threshold (yellow warning level).</summary>
        public double WarningThreshold { get; set; }
        
        /// <summary>Critical threshold (red critical level).</summary>
        public double CriticalThreshold { get; set; }
        
        /// <summary>Scaling factor for analog to digital conversion.</summary>
        public double ScalingFactor { get; set; } = 1.0;
        
        /// <summary>Offset for analog to digital conversion.</summary>
        public double Offset { get; set; } = 0.0;
        
        /// <summary>Timestamp of the last reading.</summary>
        public DateTime LastUpdateTime { get; set; } = DateTime.UtcNow;
        
        /// <summary>Alias for LastUpdateTime for test compatibility.</summary>
        public DateTime LastUpdated => LastUpdateTime;
        
        /// <summary>ECU Module ID this channel belongs to.</summary>
        public string EcuModuleId { get; set; } = string.Empty;
        
        /// <summary>History of recent values (max 60 samples).</summary>
        public IEnumerable<double> ValueHistory => _valueHistory.ToArray();
        
        /// <summary>
        /// Returns the value normalized to 0-1 range based on MinValue/MaxValue.
        /// </summary>
        public double NormalizedValue
        {
            get
            {
                var range = MaxValue - MinValue;
                if (range == 0) return 0;
                var normalized = (CurrentValue - MinValue) / range;
                return Math.Clamp(normalized, 0.0, 1.0);
            }
        }
        
        /// <summary>True if the sensor is currently in an alarm state.</summary>
        public bool IsInAlarm => CurrentValue > CriticalThreshold || CurrentValue < MinValue;
        
        /// <summary>True if the sensor is currently in a warning state.</summary>
        public bool IsInWarning => (CurrentValue > WarningThreshold && CurrentValue <= CriticalThreshold) || 
                                    (CurrentValue < (MinValue + (MaxValue - MinValue) * 0.1));
        
        /// <summary>
        /// Updates the sensor value and evaluates status.
        /// </summary>
        public void UpdateValue(double newValue)
        {
            CurrentValue = newValue;
            LastUpdateTime = DateTime.UtcNow;
            
            // Add to history (thread-safe, capped at MaxHistorySize)
            _valueHistory.Enqueue(newValue);
            while (_valueHistory.Count > MaxHistorySize)
                _valueHistory.TryDequeue(out _);
            
            // Evaluate status based on thresholds
            if (CurrentValue > CriticalThreshold || CurrentValue < MinValue)
                Status = ChannelStatus.Fault;
            else if (CurrentValue > WarningThreshold)
                Status = ChannelStatus.Warning;
            else
                Status = ChannelStatus.Active;
        }
        
        /// <summary>
        /// Validates if the current value is within the acceptable range.
        /// </summary>
        public bool IsValueInRange()
        {
            return CurrentValue >= MinValue && CurrentValue <= MaxValue;
        }
        
        /// <summary>
        /// Checks if the value exceeds warning or critical thresholds.
        /// </summary>
        public bool IsValueValid()
        {
            return CurrentValue >= MinValue && CurrentValue <= MaxValue;
        }
        
        /// <summary>
        /// Returns the severity level as a string.
        /// </summary>
        public string GetSeverity()
        {
            if (CurrentValue > CriticalThreshold || CurrentValue < MinValue)
                return "CRITICAL";
            if (IsInWarning)
                return "WARNING";
            return "NORMAL";
        }
        
        /// <summary>
        /// Returns a formatted string representation of the sensor channel.
        /// </summary>
        public override string ToString() =>
            $"[{Id}] {Name}: {CurrentValue}{Unit} ({Status}) - Severity: {GetSeverity()}";
    }
}