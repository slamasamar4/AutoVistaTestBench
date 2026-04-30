namespace AutoVistaTestBench.Core.Enums
{
    /// <summary>
    /// Defines the physical measurement type of a sensor channel.
    /// Common types found in automotive ECU test benches.
    /// </summary>
    public enum SensorType
    {
        /// <summary>Temperature sensor (e.g., NTC thermistor, thermocouple). Unit: °C</summary>
        Temperature,

        /// <summary>Voltage measurement channel. Unit: V</summary>
        Voltage,

        /// <summary>Current measurement channel. Unit: A</summary>
        Current,

        /// <summary>Rotational speed sensor (e.g., crankshaft, wheel speed). Unit: RPM</summary>
        Speed,

        /// <summary>Pressure sensor (e.g., oil pressure, boost pressure). Unit: bar</summary>
        Pressure,

        /// <summary>Digital I/O channel. Unit: boolean (0/1)</summary>
        DigitalIO,

        /// <summary>CAN bus message channel. Unit: raw bytes</summary>
        CanBus
    }
}