using AutoVistaTestBench.Core.Enums;
using AutoVistaTestBench.Core.Interfaces;
using AutoVistaTestBench.Core.Models;
using AutoVistaTestBench.Simulator.Generators;
using Microsoft.Extensions.Logging;

namespace AutoVistaTestBench.Simulator
{
    /// <summary>
    /// Main hardware simulator that orchestrates all ECU module simulators.
    /// 
    /// Architecture note: This class runs a dedicated background thread (not Task/async)
    /// because real hardware acquisition loops must be time-deterministic.
    /// In production, this thread would be replaced by a hardware interrupt callback
    /// or a timer-triggered DMA buffer callback from the driver SDK.
    /// 
    /// Thread safety: All public events are raised on the simulator thread.
    /// WPF UI must dispatch updates to the UI thread using Dispatcher.
    /// </summary>
    public class HardwareSimulator : IHardwareSimulator, IDisposable
    {
        private readonly ILogger<HardwareSimulator> _logger;
        private readonly List<EcuModuleSimulator> _moduleSimulators = new();
        private readonly CanFrameGenerator _canFrameGenerator;
        private readonly FaultInjector _globalFaultInjector;

        private Thread? _acquisitionThread;
        private volatile bool _isRunning;
        private bool _disposed;

        // Acquisition rate: 100ms per tick = 10Hz update rate
        // In production HIL systems, rates of 1kHz–10kHz are common
        private const int AcquisitionIntervalMs = 100;

        public IReadOnlyList<EcuModule> Modules =>
            _moduleSimulators.Select(s => s.Module).ToList().AsReadOnly();

        public bool IsRunning => _isRunning;

        public event EventHandler<SensorChannel>? ChannelUpdated;
        public event EventHandler<CanFrame>? CanFrameReceived;
        public event EventHandler<AnomalyReport>? AnomalyDetected;

        public HardwareSimulator(ILogger<HardwareSimulator> logger)
        {
            _logger = logger;
            _canFrameGenerator = new CanFrameGenerator(seed: 42);
            _globalFaultInjector = new FaultInjector(seed: 13);

            // Build the simulated ECU module configuration
            InitializeModules();
        }

        /// <summary>
        /// Defines the simulated test bench configuration with realistic ECU modules.
        /// In production, this would be loaded from a device configuration XML/JSON file.
        /// </summary>
        private void InitializeModules()
        {
            // ── Module 1: Powertrain ECU ──────────────────────────────────────────────
            var powertrainEcu = new EcuModule
            {
                Id = "ECU_POWERTRAIN",
                Name = "Powertrain Control Module",
                FirmwareVersion = "3.7.2",
                SerialNumber = "PTR-2024-001",
                CanNodeAddress = 0x10,
                Channels = new List<SensorChannel>
                {
                    new SensorChannel
                    {
                        Id = "PT_TEMP_01", Name = "Engine Coolant Temperature",
                        Type = SensorType.Temperature, Unit = "°C",
                        MinValue = -40, MaxValue = 130, CurrentValue = 25,
                        WarningThreshold = 100, FaultThreshold = 120,
                        EcuModuleId = "ECU_POWERTRAIN"
                    },
                    new SensorChannel
                    {
                        Id = "PT_TEMP_02", Name = "Oil Temperature",
                        Type = SensorType.Temperature, Unit = "°C",
                        MinValue = -20, MaxValue = 150, CurrentValue = 30,
                        WarningThreshold = 120, FaultThreshold = 140,
                        EcuModuleId = "ECU_POWERTRAIN"
                    },
                    new SensorChannel
                    {
                        Id = "PT_RPM_01", Name = "Engine Speed",
                        Type = SensorType.Speed, Unit = "RPM",
                        MinValue = 0, MaxValue = 8000, CurrentValue = 800,
                        WarningThreshold = 6500, FaultThreshold = 7500,
                        EcuModuleId = "ECU_POWERTRAIN"
                    },
                    new SensorChannel
                    {
                        Id = "PT_PRES_01", Name = "Oil Pressure",
                        Type = SensorType.Pressure, Unit = "bar",
                        MinValue = 0, MaxValue = 8, CurrentValue = 3.5,
                        WarningThreshold = 1.5, FaultThreshold = 0.8,
                        EcuModuleId = "ECU_POWERTRAIN"
                    }
                }
            };

            // ── Module 2: Battery Management System ───────────────────────────────────
            var bmsEcu = new EcuModule
            {
                Id = "ECU_BMS",
                Name = "Battery Management System",
                FirmwareVersion = "2.1.0",
                SerialNumber = "BMS-2024-007",
                CanNodeAddress = 0x20,
                Channels = new List<SensorChannel>
                {
                    new SensorChannel
                    {
                        Id = "BMS_VOLT_01", Name = "Battery Voltage",
                        Type = SensorType.Voltage, Unit = "V",
                        MinValue = 9.0, MaxValue = 16.0, CurrentValue = 13.8,
                        WarningThreshold = 15.0, FaultThreshold = 15.8,
                        EcuModuleId = "ECU_BMS"
                    },
                    new SensorChannel
                    {
                        Id = "BMS_VOLT_02", Name = "Alternator Output Voltage",
                        Type = SensorType.Voltage, Unit = "V",
                        MinValue = 0, MaxValue = 16.0, CurrentValue = 14.1,
                        WarningThreshold = 14.8, FaultThreshold = 15.5,
                        EcuModuleId = "ECU_BMS"
                    },
                    new SensorChannel
                    {
                        Id = "BMS_CURR_01", Name = "Battery Current",
                        Type = SensorType.Current, Unit = "A",
                        MinValue = -100, MaxValue = 200, CurrentValue = 5.0,
                        WarningThreshold = 150, FaultThreshold = 180,
                        EcuModuleId = "ECU_BMS"
                    },
                    new SensorChannel
                    {
                        Id = "BMS_TEMP_01", Name = "Battery Temperature",
                        Type = SensorType.Temperature, Unit = "°C",
                        MinValue = -20, MaxValue = 60, CurrentValue = 22,
                        WarningThreshold = 50, FaultThreshold = 58,
                        EcuModuleId = "ECU_BMS"
                    }
                }
            };

            // ── Module 3: Body Control Module ─────────────────────────────────────────
            var bcmEcu = new EcuModule
            {
                Id = "ECU_BCM",
                Name = "Body Control Module",
                FirmwareVersion = "1.9.4",
                SerialNumber = "BCM-2024-003",
                CanNodeAddress = 0x30,
                Channels = new List<SensorChannel>
                {
                    new SensorChannel
                    {
                        Id = "BCM_VOLT_01", Name = "Interior 12V Rail",
                        Type = SensorType.Voltage, Unit = "V",
                        MinValue = 9.0, MaxValue = 15.0, CurrentValue = 12.6,
                        WarningThreshold = 14.5, FaultThreshold = 14.9,
                        EcuModuleId = "ECU_BCM"
                    },
                    new SensorChannel
                    {
                        Id = "BCM_TEMP_01", Name = "Ambient Temperature",
                        Type = SensorType.Temperature, Unit = "°C",
                        MinValue = -40, MaxValue = 85, CurrentValue = 23,
                        WarningThreshold = 70, FaultThreshold = 80,
                        EcuModuleId = "ECU_BCM"
                    }
                }
            };

            _moduleSimulators.Add(new EcuModuleSimulator(powertrainEcu));
            _moduleSimulators.Add(new EcuModuleSimulator(bmsEcu));
            _moduleSimulators.Add(new EcuModuleSimulator(bcmEcu));

            _logger.LogInformation("Hardware simulator initialized with {Count} ECU modules", _moduleSimulators.Count);
        }

        /// <summary>
        /// Starts the acquisition thread.
        /// Uses a dedicated Thread (not ThreadPool/Task) for deterministic timing.
        /// Thread priority is set to AboveNormal to improve timing accuracy.
        /// </summary>
        public void Start()
        {
            if (_isRunning)
            {
                _logger.LogWarning("HardwareSimulator.Start() called while already running — ignoring");
                return;
            }

            _isRunning = true;

            // Mark all channels as Active (were Idle before start)
            foreach (var sim in _moduleSimulators)
                foreach (var ch in sim.Module.Channels)
                    ch.Status = ChannelStatus.Active;

            _acquisitionThread = new Thread(AcquisitionLoop)
            {
                Name = "HW_AcquisitionThread",
                IsBackground = true,                    // Will not prevent app from exiting
                Priority = ThreadPriority.AboveNormal   // Higher priority for timing accuracy
            };

            _acquisitionThread.Start();
            _logger.LogInformation("Hardware simulator started — acquisition at {Rate}Hz",
                1000 / AcquisitionIntervalMs);
        }

        /// <summary>
        /// Stops the acquisition thread gracefully.
        /// Sets the volatile _isRunning flag to false and waits for thread termination.
        /// </summary>
        public void Stop()
        {
            if (!_isRunning) return;

            _isRunning = false;
            _acquisitionThread?.Join(timeout: TimeSpan.FromSeconds(3));

            // Reset all channels to Idle
            foreach (var sim in _moduleSimulators)
            {
                sim.Module.IsConnected = false;
                foreach (var ch in sim.Module.Channels)
                    ch.Status = ChannelStatus.Idle;
            }

            _logger.LogInformation("Hardware simulator stopped");
        }

        /// <summary>
        /// The main acquisition loop running on the dedicated hardware thread.
        /// 
        /// Design: Uses Thread.Sleep for interval control.
        /// In production: Replace with a hardware timer callback or
        /// a Stopwatch-based spin-wait for sub-millisecond accuracy.
        /// </summary>
        private void AcquisitionLoop()
        {
            _logger.LogDebug("Acquisition loop started on thread: {ThreadName}", Thread.CurrentThread.Name);

            while (_isRunning)
            {
                var tickStart = DateTime.UtcNow;

                try
                {
                    // Update all ECU module simulators
                    foreach (var moduleSim in _moduleSimulators)
                    {
                        foreach (var (channel, isFault) in moduleSim.Tick())
                        {
                            // Raise event for each updated channel
                            ChannelUpdated?.Invoke(this, channel);

                            // If this tick caused a new fault, generate an anomaly report
                            if (isFault)
                            {
                                var anomaly = new AnomalyReport
                                {
                                    ChannelId = channel.Id,
                                    ChannelName = channel.Name,
                                    TriggerValue = channel.CurrentValue,
                                    ThresholdValue = channel.FaultThreshold,
                                    Description = $"Fault threshold exceeded on {channel.Name}. " +
                                                  $"Measured: {channel.CurrentValue:F3} {channel.Unit}, " +
                                                  $"Threshold: {channel.FaultThreshold:F3} {channel.Unit}",
                                    DetectedAt = DateTime.UtcNow
                                };
                                AnomalyDetected?.Invoke(this, anomaly);
                                moduleSim.Module.ErrorCount++;
                            }
                        }
                    }

                    // Generate CAN bus traffic from the first (powertrain) module
                    if (_moduleSimulators.Count > 0)
                    {
                        foreach (var frame in _canFrameGenerator.GenerateFrames("ECU_POWERTRAIN"))
                        {
                            CanFrameReceived?.Invoke(this, frame);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in acquisition loop — continuing");
                }

                // Calculate remaining sleep time to maintain target interval
                var elapsed = (DateTime.UtcNow - tickStart).TotalMilliseconds;
                var sleepMs = Math.Max(0, AcquisitionIntervalMs - elapsed);
                Thread.Sleep((int)sleepMs);
            }

            _logger.LogDebug("Acquisition loop exited");
        }

        public void Dispose()
        {
            if (_disposed) return;
            Stop();
            _disposed = true;
        }
    }
}