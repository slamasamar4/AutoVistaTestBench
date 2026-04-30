using System.Collections.ObjectModel;
using System.Windows;
using AutoVistaTestBench.Core.Interfaces;
using AutoVistaTestBench.Core.Models;
using AutoVistaTestBench.Infrastructure;

namespace AutoVistaTestBench.ViewModels
{
    /// <summary>
    /// ViewModel for the main dashboard view.
    /// Displays overall system status, active session metrics, and ECU module health.
    /// 
    /// Threading note: All event handlers from IDataAcquisitionService arrive on
    /// the simulator's background thread. We use Dispatcher.Invoke to marshal
    /// updates to the WPF UI thread before modifying ObservableCollections.
    /// </summary>
    public class DashboardViewModel : ViewModelBase
    {
        private readonly IDataAcquisitionService _acquisitionService;

        // ── Bound Properties ──────────────────────────────────────────────────────

        private string _sessionName = "Session_001";
        public string SessionName
        {
            get => _sessionName;
            set => SetProperty(ref _sessionName, value);
        }

        private string _operatorName = "Engineer";
        public string OperatorName
        {
            get => _operatorName;
            set => SetProperty(ref _operatorName, value);
        }

        private string _vehicleId = "VIN-TEST-001";
        public string VehicleId
        {
            get => _vehicleId;
            set => SetProperty(ref _vehicleId, value);
        }

        private string _sessionStatus = "Idle";
        public string SessionStatus
        {
            get => _sessionStatus;
            set => SetProperty(ref _sessionStatus, value);
        }

        private string _sessionDuration = "00:00:00";
        public string SessionDuration
        {
            get => _sessionDuration;
            set => SetProperty(ref _sessionDuration, value);
        }

        private long _totalSamples;
        public long TotalSamples
        {
            get => _totalSamples;
            set => SetProperty(ref _totalSamples, value);
        }

        private int _faultCount;
        public int FaultCount
        {
            get => _faultCount;
            set => SetProperty(ref _faultCount, value);
        }

        private int _warningCount;
        public int WarningCount
        {
            get => _warningCount;
            set => SetProperty(ref _warningCount, value);
        }

        private bool _isRunning;
        public bool IsRunning
        {
            get => _isRunning;
            set
            {
                SetProperty(ref _isRunning, value);
                OnPropertyChanged(nameof(CanStart));
                OnPropertyChanged(nameof(CanStop));
            }
        }

        public bool CanStart => !_isRunning;
        public bool CanStop => _isRunning;

        private string _overallHealth = "OFFLINE";
        public string OverallHealth
        {
            get => _overallHealth;
            set => SetProperty(ref _overallHealth, value);
        }

        private System.Windows.Media.SolidColorBrush _healthColor =
            new(System.Windows.Media.Color.FromRgb(0x95, 0xA5, 0xA6));
        public System.Windows.Media.SolidColorBrush HealthColor
        {
            get => _healthColor;
            set => SetProperty(ref _healthColor, value);
        }

        // ── Commands ──────────────────────────────────────────────────────────────

        public RelayCommand StartSessionCommand { get; }
        public RelayCommand StopSessionCommand { get; }

        // ── ECU Module Status Collection ──────────────────────────────────────────

        public ObservableCollection<EcuModuleStatusViewModel> ModuleStatuses { get; } = new();

        // ── Recent Anomalies (for dashboard alert panel) ──────────────────────────

        public ObservableCollection<AnomalyReport> RecentAnomalies { get; } = new();

        // ── Timer for duration display ────────────────────────────────────────────
        private System.Windows.Threading.DispatcherTimer? _durationTimer;

        public DashboardViewModel(IDataAcquisitionService acquisitionService)
        {
            _acquisitionService = acquisitionService;

            StartSessionCommand = new RelayCommand(
                execute: async () => await StartSessionAsync(),
                canExecute: () => CanStart);

            StopSessionCommand = new RelayCommand(
                execute: async () => await StopSessionAsync(),
                canExecute: () => CanStop);

            // Subscribe to acquisition service events
            _acquisitionService.ChannelUpdated += OnChannelUpdated;
            _acquisitionService.AnomalyDetected += OnAnomalyDetected;

            // Initialize ECU module status entries
            InitializeModuleStatuses();
        }

        private void InitializeModuleStatuses()
        {
            foreach (var module in _acquisitionService.Modules)
            {
                ModuleStatuses.Add(new EcuModuleStatusViewModel(module));
            }
        }

        private async Task StartSessionAsync()
        {
            try
            {
                await _acquisitionService.StartSessionAsync(
                    SessionName, OperatorName, VehicleId);

                IsRunning = true;
                SessionStatus = "ACQUIRING";
                FaultCount = 0;
                WarningCount = 0;
                TotalSamples = 0;
                RecentAnomalies.Clear();
                UpdateHealthIndicator();

                // Start the duration display timer
                _durationTimer = new System.Windows.Threading.DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(1)
                };
                _durationTimer.Tick += (s, e) =>
                {
                    if (_acquisitionService.CurrentSession != null)
                    {
                        SessionDuration = _acquisitionService.CurrentSession.Duration.ToString(@"hh\:mm\:ss");
                        TotalSamples = _acquisitionService.CurrentSession.TotalSamples;
                        FaultCount = _acquisitionService.CurrentSession.FaultCount;
                        WarningCount = _acquisitionService.CurrentSession.WarningCount;
                        UpdateHealthIndicator();
                    }
                };
                _durationTimer.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to start session:\n{ex.Message}",
                    "Session Start Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task StopSessionAsync()
        {
            _durationTimer?.Stop();
            await _acquisitionService.StopSessionAsync();

            IsRunning = false;
            SessionStatus = "STOPPED";
            UpdateHealthIndicator();
        }

        private void OnChannelUpdated(object? sender, Core.Models.SensorChannel channel)
        {
            // Update module status on UI thread
            Application.Current?.Dispatcher.Invoke(() =>
            {
                var moduleStatus = ModuleStatuses.FirstOrDefault(m => m.ModuleId == channel.EcuModuleId);
                moduleStatus?.Refresh(_acquisitionService.Modules
                    .FirstOrDefault(m => m.Id == channel.EcuModuleId));
            });
        }

        private void OnAnomalyDetected(object? sender, AnomalyReport anomaly)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                RecentAnomalies.Insert(0, anomaly);

                // Keep display limited to most recent 20 anomalies
                while (RecentAnomalies.Count > 20)
                    RecentAnomalies.RemoveAt(RecentAnomalies.Count - 1);
            });
        }

        private void UpdateHealthIndicator()
        {
            if (!IsRunning)
            {
                OverallHealth = "OFFLINE";
                HealthColor = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0x95, 0xA5, 0xA6));
            }
            else if (FaultCount > 0)
            {
                OverallHealth = "FAULT";
                HealthColor = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0xE7, 0x4C, 0x3C));
            }
            else if (WarningCount > 0)
            {
                OverallHealth = "WARNING";
                HealthColor = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0xF3, 0x9C, 0x12));
            }
            else
            {
                OverallHealth = "NOMINAL";
                HealthColor = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0x2E, 0xCC, 0x71));
            }
        }
    }

    /// <summary>
    /// Lightweight ViewModel for displaying ECU module status in the dashboard.
    /// </summary>
    public class EcuModuleStatusViewModel : ViewModelBase
    {
        private EcuModule? _module;

        public string ModuleId { get; }
        public string ModuleName { get; }

        private string _status = "IDLE";
        public string Status
        {
            get => _status;
            private set => SetProperty(ref _status, value);
        }

        private System.Windows.Media.SolidColorBrush _statusColor =
            new(System.Windows.Media.Color.FromRgb(0x95, 0xA5, 0xA6));
        public System.Windows.Media.SolidColorBrush StatusColor
        {
            get => _statusColor;
            private set => SetProperty(ref _statusColor, value);
        }

        private int _errorCount;
        public int ErrorCount
        {
            get => _errorCount;
            private set => SetProperty(ref _errorCount, value);
        }

        private string _firmwareVersion = "--";
        public string FirmwareVersion
        {
            get => _firmwareVersion;
            private set => SetProperty(ref _firmwareVersion, value);
        }

        public EcuModuleStatusViewModel(EcuModule module)
        {
            _module = module;
            ModuleId = module.Id;
            ModuleName = module.Name;
            FirmwareVersion = module.FirmwareVersion;
        }

        public void Refresh(EcuModule? module)
        {
            if (module == null) return;
            _module = module;

            Status = module.Status.ToString().ToUpperInvariant();
            ErrorCount = module.ErrorCount;

            StatusColor = module.Status switch
            {
                Core.Enums.ChannelStatus.Active =>
                    new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x2E, 0xCC, 0x71)),
                Core.Enums.ChannelStatus.Warning =>
                    new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xF3, 0x9C, 0x12)),
                Core.Enums.ChannelStatus.Fault =>
                    new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xE7, 0x4C, 0x3C)),
                _ =>
                    new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x95, 0xA5, 0xA6))
            };
        }
    }
}