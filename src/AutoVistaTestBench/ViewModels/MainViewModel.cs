using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AutoVistaTestBench.ViewModels
{
    /// <summary>
    /// Root ViewModel for the MainWindow.
    /// Acts as a navigation coordinator between the different views/tabs.
    /// </summary>
    public class MainViewModel : INotifyPropertyChanged
    {
        public DashboardViewModel DashboardViewModel { get; }
        public ChannelMonitorViewModel ChannelMonitorViewModel { get; }
        public LogAnalyzerViewModel LogAnalyzerViewModel { get; }

        private int _selectedTabIndex;
        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set { _selectedTabIndex = value; OnPropertyChanged(); }
        }

        public string ApplicationTitle =>
            $"AutoVista ECU Test Bench — v1.0.0 | .NET 8 / WPF";

        public MainViewModel()
        {
            DashboardViewModel = new DashboardViewModel();
            ChannelMonitorViewModel = new ChannelMonitorViewModel();
            LogAnalyzerViewModel = new LogAnalyzerViewModel();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}