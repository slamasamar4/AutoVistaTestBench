using AutoVistaTestBench.Infrastructure;

namespace AutoVistaTestBench.ViewModels
{
    /// <summary>
    /// Root ViewModel for the MainWindow.
    /// Acts as a navigation coordinator between the different views/tabs.
    /// Holds references to child ViewModels injected via DI.
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        public DashboardViewModel DashboardViewModel { get; }
        public ChannelMonitorViewModel ChannelMonitorViewModel { get; }
        public LogAnalyzerViewModel LogAnalyzerViewModel { get; }

        private int _selectedTabIndex;
        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set => SetProperty(ref _selectedTabIndex, value);
        }

        public string ApplicationTitle =>
            $"AutoVista ECU Test Bench — v1.0.0 | .NET 8 / WPF";

        public MainViewModel(
            DashboardViewModel dashboardViewModel,
            ChannelMonitorViewModel channelMonitorViewModel,
            LogAnalyzerViewModel logAnalyzerViewModel)
        {
            DashboardViewModel = dashboardViewModel;
            ChannelMonitorViewModel = channelMonitorViewModel;
            LogAnalyzerViewModel = logAnalyzerViewModel;
        }
    }
}