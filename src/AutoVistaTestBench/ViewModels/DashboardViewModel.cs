using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;

namespace AutoVistaTestBench.ViewModels
{
    /// <summary>
    /// ViewModel for the main dashboard view.
    /// Displays overall system status, active session metrics, and ECU module health.
    /// </summary>
    public class DashboardViewModel : INotifyPropertyChanged
    {
        // ── Bound Properties ──────────────────────────────────────────────────────

        private string _sessionName = "Session_001";
        public string SessionName
        {
            get => _sessionName;
            set { _sessionName = value; OnPropertyChanged(); }
        }

        private string _operatorName = "Engineer";
        public string OperatorName
        {
            get => _operatorName;
            set { _operatorName = value; OnPropertyChanged(); }
        }

        private string _vehicleId = "VIN-TEST-001";
        public string VehicleId
        {
            get => _vehicleId;
            set { _vehicleId = value; OnPropertyChanged(); }
        }

        private string _sessionStatus = "Idle";
        public string SessionStatus
        {
            get => _sessionStatus;
            set { _sessionStatus = value; OnPropertyChanged(); }
        }

        private string _sessionDuration = "00:00:00";
        public string SessionDuration
        {
            get => _sessionDuration;
            set { _sessionDuration = value; OnPropertyChanged(); }
        }

        private long _totalSamples;
        public long TotalSamples
        {
            get => _totalSamples;
            set { _totalSamples = value; OnPropertyChanged(); }
        }

        private int _faultCount;
        public int FaultCount
        {
            get => _faultCount;
            set { _faultCount = value; OnPropertyChanged(); }
        }

        private int _warningCount;
        public int WarningCount
        {
            get => _warningCount;
            set { _warningCount = value; OnPropertyChanged(); }
        }

        private bool _isRunning;
        public bool IsRunning
        {
            get => _isRunning;
            set 
            { 
                _isRunning = value; 
                OnPropertyChanged();
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
            set { _overallHealth = value; OnPropertyChanged(); }
        }

        private SolidColorBrush _healthColor = new SolidColorBrush(Color.FromRgb(0x95, 0xA5, 0xA6));
        public SolidColorBrush HealthColor
        {
            get => _healthColor;
            set { _healthColor = value; OnPropertyChanged(); }
        }

        // ── ECU Module Status Collection ──────────────────────────────────────────

        public ObservableCollection<EcuModuleStatusViewModel> ModuleStatuses { get; } = new();

        // ── Recent Anomalies ──────────────────────────────────────────

        public ObservableCollection<object> RecentAnomalies { get; } = new();

        // ── Commands ──────────────────────────────────────────────────────────────

        public RelayCommand StartSessionCommand { get; }
        public RelayCommand StopSessionCommand { get; }

        public DashboardViewModel()
        {
            StartSessionCommand = new RelayCommand(
                execute: () => StartSession(),
                canExecute: () => CanStart);

            StopSessionCommand = new RelayCommand(
                execute: () => StopSession(),
                canExecute: () => CanStop);

            // Initialize with sample module statuses
            InitializeModuleStatuses();
        }

        private void InitializeModuleStatuses()
        {
            ModuleStatuses.Add(new EcuModuleStatusViewModel 
            { 
                ModuleId = "ECU_01", 
                ModuleName = "Powertrain Control", 
                Status = "ACTIVE", 
                FirmwareVersion = "2.1.0", 
                ErrorCount = 0 
            });
            
            ModuleStatuses.Add(new EcuModuleStatusViewModel 
            { 
                ModuleId = "ECU_02", 
                ModuleName = "Body Control", 
                Status = "ACTIVE", 
                FirmwareVersion = "1.8.3", 
                ErrorCount = 2 
            });
            
            ModuleStatuses.Add(new EcuModuleStatusViewModel 
            { 
                ModuleId = "ECU_03", 
                ModuleName = "Battery Management", 
                Status = "WARNING", 
                FirmwareVersion = "3.0.1", 
                ErrorCount = 1 
            });
        }

        private void StartSession()
        {
            IsRunning = true;
            SessionStatus = "ACQUIRING";
            FaultCount = 0;
            WarningCount = 0;
            TotalSamples = 0;
            UpdateHealthIndicator();
        }

        private void StopSession()
        {
            IsRunning = false;
            SessionStatus = "STOPPED";
            UpdateHealthIndicator();
        }

        private void UpdateHealthIndicator()
        {
            if (!IsRunning)
            {
                OverallHealth = "OFFLINE";
                HealthColor = new SolidColorBrush(Color.FromRgb(0x95, 0xA5, 0xA6));
            }
            else if (FaultCount > 0)
            {
                OverallHealth = "FAULT";
                HealthColor = new SolidColorBrush(Color.FromRgb(0xE7, 0x4C, 0x3C));
            }
            else if (WarningCount > 0)
            {
                OverallHealth = "WARNING";
                HealthColor = new SolidColorBrush(Color.FromRgb(0xF3, 0x9C, 0x12));
            }
            else
            {
                OverallHealth = "NOMINAL";
                HealthColor = new SolidColorBrush(Color.FromRgb(0x2E, 0xCC, 0x71));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// Lightweight ViewModel for displaying ECU module status in the dashboard.
    /// </summary>
    public class EcuModuleStatusViewModel : INotifyPropertyChanged
    {
        private string _moduleId = string.Empty;
        private string _moduleName = string.Empty;
        private string _status = "IDLE";
        private string _firmwareVersion = "--";
        private int _errorCount;
        private SolidColorBrush _statusColor = new SolidColorBrush(Color.FromRgb(0x95, 0xA5, 0xA6));

        public string ModuleId
        {
            get => _moduleId;
            set { _moduleId = value; OnPropertyChanged(); }
        }

        public string ModuleName
        {
            get => _moduleName;
            set { _moduleName = value; OnPropertyChanged(); }
        }

        public string Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(); }
        }

        public string FirmwareVersion
        {
            get => _firmwareVersion;
            set { _firmwareVersion = value; OnPropertyChanged(); }
        }

        public int ErrorCount
        {
            get => _errorCount;
            set { _errorCount = value; OnPropertyChanged(); }
        }

        public SolidColorBrush StatusColor
        {
            get => _statusColor;
            set { _statusColor = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}