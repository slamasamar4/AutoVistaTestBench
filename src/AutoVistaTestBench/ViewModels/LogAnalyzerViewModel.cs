using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;

namespace AutoVistaTestBench.ViewModels
{
    /// <summary>
    /// ViewModel for the log viewer and AI analysis panel.
    /// </summary>
    public class LogAnalyzerViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<LogEntryViewModel> _logEntries = new();
        private string _aiAnalysisResult = "Click 'Analyze Logs with AI' to run AI-powered diagnostic analysis.";
        private bool _isAnalyzing;
        private string _logFilePath = "No active session";
        private string _minimumSeverityFilter = "Info";

        public ObservableCollection<LogEntryViewModel> LogEntries
        {
            get => _logEntries;
            set
            {
                _logEntries = value;
                OnPropertyChanged();
            }
        }

        public string AiAnalysisResult
        {
            get => _aiAnalysisResult;
            set
            {
                _aiAnalysisResult = value;
                OnPropertyChanged();
            }
        }

        public bool IsAnalyzing
        {
            get => _isAnalyzing;
            set
            {
                _isAnalyzing = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanAnalyze));
            }
        }

        public bool CanAnalyze => !_isAnalyzing;

        public string LogFilePath
        {
            get => _logFilePath;
            set
            {
                _logFilePath = value;
                OnPropertyChanged();
            }
        }

        public string MinimumSeverityFilter
        {
            get => _minimumSeverityFilter;
            set
            {
                _minimumSeverityFilter = value;
                OnPropertyChanged();
                RefreshFilteredEntries();
            }
        }

        public ObservableCollection<string> AvailableSeverities { get; } = new()
        {
            "Debug", "Info", "Warning", "Error", "Critical"
        };

        public RelayCommand AnalyzeWithAiCommand { get; }
        public RelayCommand ClearLogCommand { get; }

        public LogAnalyzerViewModel()
        {
            AnalyzeWithAiCommand = new RelayCommand(
                execute: async () => await AnalyzeWithAiAsync(),
                canExecute: () => CanAnalyze);

            ClearLogCommand = new RelayCommand(
                execute: () => LogEntries.Clear());

            // Initialize with sample log entries
            InitializeSampleLogs();
        }

        private void InitializeSampleLogs()
        {
            LogEntries.Add(new LogEntryViewModel
            {
                Timestamp = DateTime.Now.AddSeconds(-5),
                Severity = "Info",
                Source = "System",
                Message = "Application started successfully"
            });

            LogEntries.Add(new LogEntryViewModel
            {
                Timestamp = DateTime.Now.AddSeconds(-10),
                Severity = "Warning",
                Source = "ECU_01",
                Message = "Engine temperature approaching threshold"
            });

            LogEntries.Add(new LogEntryViewModel
            {
                Timestamp = DateTime.Now.AddSeconds(-15),
                Severity = "Info",
                Source = "Simulator",
                Message = "Data acquisition initialized"
            });

            LogEntries.Add(new LogEntryViewModel
            {
                Timestamp = DateTime.Now.AddSeconds(-20),
                Severity = "Error",
                Source = "ECU_03",
                Message = "Battery voltage below nominal range"
            });
        }

        private void RefreshFilteredEntries()
        {
            // Filter logic would go here
        }

        private async Task AnalyzeWithAiAsync()
        {
            IsAnalyzing = true;
            AiAnalysisResult = "⏳ Sending logs to AI for analysis...";

            try
            {
                // Simulate AI analysis delay
                await Task.Delay(2000);

                AiAnalysisResult = "AI Analysis Complete:\n\n" +
                    "Summary: No critical issues detected based on the current log data.\n\n" +
                    "Recommendations:\n" +
                    "1. Monitor engine temperature trends over time\n" +
                    "2. Schedule maintenance for ECU_03 battery system\n" +
                    "3. Continue normal operations with periodic checks\n\n" +
                    "Root Cause Analysis: The warnings in ECU_01 appear to be load-related " +
                    "and may require calibration adjustment during high-load conditions.";
            }
            catch (Exception ex)
            {
                AiAnalysisResult = $"AI analysis failed: {ex.Message}";
            }
            finally
            {
                IsAnalyzing = false;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class LogEntryViewModel : INotifyPropertyChanged
    {
        private DateTime _timestamp;
        private string _severity = string.Empty;
        private string _source = string.Empty;
        private string _message = string.Empty;
        private Brush _severityColor = Brushes.White;

        public DateTime Timestamp
        {
            get => _timestamp;
            set
            {
                _timestamp = value;
                OnPropertyChanged();
            }
        }

        public string Severity
        {
            get => _severity;
            set
            {
                _severity = value;
                OnPropertyChanged();

                // Set color based on severity
                switch (value)
                {
                    case "Error":
                    case "Critical":
                        SeverityColor = new SolidColorBrush(Color.FromRgb(0xE7, 0x4C, 0x3C));
                        break;
                    case "Warning":
                        SeverityColor = new SolidColorBrush(Color.FromRgb(0xF3, 0x9C, 0x12));
                        break;
                    case "Info":
                        SeverityColor = new SolidColorBrush(Color.FromRgb(0x4A, 0x9E, 0xFF));
                        break;
                    default:
                        SeverityColor = new SolidColorBrush(Color.FromRgb(0x95, 0xA5, 0xA6));
                        break;
                }
            }
        }

        public string Source
        {
            get => _source;
            set
            {
                _source = value;
                OnPropertyChanged();
            }
        }

        public string Message
        {
            get => _message;
            set
            {
                _message = value;
                OnPropertyChanged();
            }
        }

        public Brush SeverityColor
        {
            get => _severityColor;
            set
            {
                _severityColor = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}