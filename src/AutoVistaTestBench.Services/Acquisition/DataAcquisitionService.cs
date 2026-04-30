using AutoVistaTestBench.Core.Enums;
using AutoVistaTestBench.Core.Interfaces;
using AutoVistaTestBench.Core.Models;
using Microsoft.Extensions.Logging;

namespace AutoVistaTestBench.Services.Acquisition
{
    /// <summary>
    /// Orchestrates the complete test session lifecycle:
    /// 1. Creates and manages TestSession objects
    /// 2. Subscribes to hardware simulator events
    /// 3. Routes data to the logging service
    /// 4. Re-exposes events for ViewModel consumption
    /// 
    /// This is the central service that ties together hardware, logging, and UI.
    /// It decouples the ViewModel from direct hardware simulator access.
    /// </summary>
    public class DataAcquisitionService : IDataAcquisitionService, IDisposable
    {
        private readonly IHardwareSimulator _simulator;
        private readonly ILoggingService _loggingService;
        private readonly ICanBusService _canBusService;
        private readonly ILogger<DataAcquisitionService> _logger;

        private TestSession? _currentSession;
        private bool _isAcquiring;
        private bool _disposed;

        public TestSession? CurrentSession => _currentSession;
        public bool IsAcquiring => _isAcquiring;
        public IReadOnlyList<EcuModule> Modules => _simulator.Modules;

        public event EventHandler<SensorChannel>? ChannelUpdated;
        public event EventHandler<CanFrame>? CanFrameReceived;
        public event EventHandler<AnomalyReport>? AnomalyDetected;
        public event EventHandler<LogEntry>? LogEntryAdded;

        public DataAcquisitionService(
            IHardwareSimulator simulator,
            ILoggingService loggingService,
            ICanBusService canBusService,
            ILogger<DataAcquisitionService> logger)
        {
            _simulator = simulator;
            _loggingService = loggingService;
            _canBusService = canBusService;
            _logger = logger;

            // Subscribe to simulator events
            _simulator.ChannelUpdated += OnChannelUpdated;
            _simulator.CanFrameReceived += OnCanFrameReceived;
            _simulator.AnomalyDetected += OnAnomalyDetected;
        }

        /// <summary>
        /// Starts a new acquisition session.
        /// Creates session metadata, opens log file, and starts hardware simulator.
        /// </summary>
        public async Task StartSessionAsync(string sessionName, string operatorName, string vehicleId)
        {
            if (_isAcquiring)
                throw new InvalidOperationException("A session is already running. Stop it first.");

            _currentSession = new TestSession
            {
                SessionName = sessionName,
                OperatorName = operatorName,
                VehicleId = vehicleId,
                StartTime = DateTime.UtcNow
            };

            _logger.LogInformation("Starting session: {SessionName} | Operator: {Operator} | Vehicle: {Vehicle}",
                sessionName, operatorName, vehicleId);

            // Open the log file before starting acquisition
            await _loggingService.OpenSessionLogAsync(_currentSession);
            await _loggingService.WriteAsync(LogSeverity.Info, "DataAcquisitionService",
                $"Session started: {_currentSession.GetSummary()}");

            // Start the hardware simulator (begins background acquisition thread)
            _simulator.Start();
            _canBusService.Reset();

            _isAcquiring = true;

            _logger.LogInformation("Session {SessionId} started successfully", _currentSession.SessionId);
        }

        /// <summary>
        /// Stops the active session, finalizes logs, and stops hardware.
        /// </summary>
        public async Task StopSessionAsync()
        {
            if (!_isAcquiring || _currentSession == null)
                return;

            _simulator.Stop();
            _isAcquiring = false;

            _currentSession.EndTime = DateTime.UtcNow;

            await _loggingService.WriteAsync(LogSeverity.Info, "DataAcquisitionService",
                $"Session ended: {_currentSession.GetSummary()}");

            await _loggingService.CloseSessionLogAsync();

            _logger.LogInformation("Session {SessionId} stopped — Duration: {Duration}",
                _currentSession.SessionId, _currentSession.Duration);
        }

        /// <summary>
        /// Handles channel update events from the simulator.
        /// Updates session statistics and propagates the event to ViewModels.
        /// </summary>
        private void OnChannelUpdated(object? sender, SensorChannel channel)
        {
            if (_currentSession != null)
            {
                _currentSession.TotalSamples++;

                // Log warnings and faults (not every sample — too verbose)
                if (channel.Status == ChannelStatus.Warning)
                {
                    _ = _loggingService.WriteAsync(LogSeverity.Warning, channel.EcuModuleId,
                        $"Warning threshold exceeded on {channel.Name}",
                        channel.Id, channel.CurrentValue);
                    _currentSession.WarningCount++;
                }
            }

            // Forward event to ViewModel (will be dispatched to UI thread there)
            ChannelUpdated?.Invoke(this, channel);
        }

        /// <summary>
        /// Handles CAN frame events from the simulator.
        /// Processes the frame through the CAN bus service and forwards to ViewModels.
        /// </summary>
        private void OnCanFrameReceived(object? sender, CanFrame frame)
        {
            _canBusService.ProcessFrame(frame);
            CanFrameReceived?.Invoke(this, frame);
        }

        /// <summary>
        /// Handles anomaly events from the simulator.
        /// Logs the anomaly, updates session fault count, and forwards to ViewModels.
        /// </summary>
        private async void OnAnomalyDetected(object? sender, AnomalyReport anomaly)
        {
            if (_currentSession != null)
            {
                _currentSession.FaultCount++;
                _currentSession.AnomalyReports.Add(anomaly);
            }

            await _loggingService.WriteAsync(LogSeverity.Error, anomaly.ChannelId,
                $"FAULT DETECTED: {anomaly.Description}",
                anomaly.ChannelId, anomaly.TriggerValue);

            AnomalyDetected?.Invoke(this, anomaly);
        }

        public void Dispose()
        {
            if (_disposed) return;

            _simulator.ChannelUpdated -= OnChannelUpdated;
            _simulator.CanFrameReceived -= OnCanFrameReceived;
            _simulator.AnomalyDetected -= OnAnomalyDetected;

            if (_simulator is IDisposable disposableSimulator)
                disposableSimulator.Dispose();

            _disposed = true;
        }
    }
}