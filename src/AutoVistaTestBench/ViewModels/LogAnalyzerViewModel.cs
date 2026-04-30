using System.Collections.ObjectModel;
using System.Windows;
using AutoVistaTestBench.Core.Enums;
using AutoVistaTestBench.Core.Interfaces;
using AutoVistaTestBench.Core.Models;
using AutoVistaTestBench.Infrastructure;

namespace AutoVistaTestBench.ViewModels
{
    /// <summary>
    /// ViewModel for the log viewer and AI analysis panel.
    /// Displays live log entries during a session and provides
    /// controls to trigger AI analysis of the current log buffer.
    /// </summary>
    public class LogAnalyzerViewModel : ViewModelBase
    {
        private readonly ILoggingService _loggingService;
        private readonly IDataAcquisitionService _acquisitionService;
        private readonly IAiAnalysisService _aiAnalysisService;

        public ObservableCollection<LogEntry> LogEntries { get; } = new();

        private string _aiAnalysisResult = "Click 'Analyze Logs with AI' to run AI-powered diagnostic analysis.";
        public string AiAnalysisResult
        {
            get => _aiAnalysisResult;
            set => SetProperty(ref _aiAnalysisResult, value);
        }

        private bool _isAnalyzing;
        public bool IsAnalyzing
        {
            get => _isAnalyzing;
            set
            {
                SetProperty(ref _isAnalyzing, value);
                OnPropertyChanged(nameof(CanAnalyze));
            }
        }

        public bool CanAnalyze => !_isAnalyzing && _acquisitionService.IsAcquiring;

        private string _logFilePath = "No active session";
        public string LogFilePath
        {
            get => _logFilePath;
            set => SetProperty(ref _logFilePath, value);
        }

        private LogSeverity _minimumSeverityFilter = LogSeverity.Debug;
        public LogSeverity MinimumSeverityFilter
        {
            get => _minimumSeverityFilter;
            set
            {
                SetProperty(ref _minimumSeverityFilter, value);
                RefreshFilteredEntries();
            }
        }

        public IEnumerable<LogSeverity> AvailableSeverities =>
            Enum.GetValues<LogSeverity>();

        public RelayCommand AnalyzeWithAiCommand { get; }
        public RelayCommand ClearLogCommand { get; }

        public LogAnalyzerViewModel(
            ILoggingService loggingService,
            IDataAcquisitionService acquisitionService,
            IAiAnalysisService aiAnalysisService)
        {
            _loggingService = loggingService;
            _acquisitionService = acquisitionService;
            _aiAnalysisService = aiAnalysisService;

            AnalyzeWithAiCommand = new RelayCommand(
                execute: async () => await AnalyzeWithAiAsync(),
                canExecute: () => CanAnalyze);

            ClearLogCommand = new RelayCommand(
                execute: () => LogEntries.Clear());

            // Subscribe to log entries from acquisition service
            _acquisitionService.LogEntryAdded += OnLogEntryAdded;
            _acquisitionService.ChannelUpdated += (_, __) =>
                OnPropertyChanged(nameof(CanAnalyze));
        }

        private void OnLogEntryAdded(object? sender, LogEntry entry)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                if (entry.Severity >= _minimumSeverityFilter)
                {
                    LogEntries.Insert(0, entry); // Newest first

                    // Cap at 500 visible entries for UI performance
                    while (LogEntries.Count > 500)
                        LogEntries.RemoveAt(LogEntries.Count - 1);
                }

                if (_acquisitionService.CurrentSession?.LogFilePath is string path)
                    LogFilePath = path;
            });
        }

        private void RefreshFilteredEntries()
        {
            var entries = _loggingService.GetSessionEntries()
                .Where(e => e.Severity >= _minimumSeverityFilter)
                .OrderByDescending(e => e.Timestamp)
                .Take(500)
                .ToList();

            Application.Current?.Dispatcher.Invoke(() =>
            {
                LogEntries.Clear();
                foreach (var entry in entries)
                    LogEntries.Add(entry);
            });
        }

        private async Task AnalyzeWithAiAsync()
        {
            IsAnalyzing = true;
            AiAnalysisResult = "⏳ Sending logs to AI for analysis...";

            try
            {
                var entries = _loggingService.GetSessionEntries();
                var summary = _acquisitionService.CurrentSession?.GetSummary()
                              ?? "No active session";

                AiAnalysisResult = await _aiAnalysisService.AnalyzeLogsAsync(entries, summary);
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
    }
}