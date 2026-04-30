using System.Collections.ObjectModel;
using System.Windows;
using AutoVistaTestBench.Core.Interfaces;
using AutoVistaTestBench.Core.Models;
using AutoVistaTestBench.Infrastructure;

namespace AutoVistaTestBench.ViewModels
{
    /// <summary>
    /// ViewModel for the real-time channel monitor view.
    /// Maintains an ObservableCollection of ChannelViewModels that auto-updates
    /// as new sensor data arrives from the acquisition service.
    /// 
    /// Performance note: WPF's ObservableCollection raises CollectionChanged on every
    /// Add/Remove, which can cause performance issues with many channels.
    /// For 10–20 channels, direct binding is fine.
    /// For 100+ channels, consider virtualization or a BindingList<T>.
    /// </summary>
    public class ChannelMonitorViewModel : ViewModelBase
    {
        private readonly IDataAcquisitionService _acquisitionService;

        /// <summary>All channel ViewModels, grouped by ECU module for display.</summary>
        public ObservableCollection<ChannelViewModel> Channels { get; } = new();

        private ChannelViewModel? _selectedChannel;
        public ChannelViewModel? SelectedChannel
        {
            get => _selectedChannel;
            set => SetProperty(ref _selectedChannel, value);
        }

        private long _totalUpdateCount;
        public long TotalUpdateCount
        {
            get => _totalUpdateCount;
            set => SetProperty(ref _totalUpdateCount, value);
        }

        private string _updateRate = "0 Hz";
        public string UpdateRate
        {
            get => _updateRate;
            set => SetProperty(ref _updateRate, value);
        }

        // For computing update rate
        private long _updatesInLastSecond;
        private DateTime _lastRateCalc = DateTime.UtcNow;

        public ChannelMonitorViewModel(IDataAcquisitionService acquisitionService)
        {
            _acquisitionService = acquisitionService;

            // Pre-populate channels from all modules
            foreach (var module in _acquisitionService.Modules)
            {
                foreach (var channel in module.Channels)
                {
                    Channels.Add(new ChannelViewModel(channel));
                }
            }

            // Subscribe to live channel updates
            _acquisitionService.ChannelUpdated += OnChannelUpdated;
        }

        /// <summary>
        /// Handles incoming channel updates from the acquisition service.
        /// Updates the matching ChannelViewModel on the UI thread.
        /// 
        /// Threading: This handler is called from the simulator's background thread.
        /// We must dispatch to the UI thread before touching WPF-bound properties.
        /// </summary>
        private void OnChannelUpdated(object? sender, SensorChannel channel)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                // Find the matching ChannelViewModel by ID
                var channelVm = Channels.FirstOrDefault(c => c.Id == channel.Id);
                channelVm?.NotifyUpdate();

                // Update statistics
                TotalUpdateCount++;
                _updatesInLastSecond++;

                // Compute update rate every second
                var now = DateTime.UtcNow;
                if ((now - _lastRateCalc).TotalSeconds >= 1.0)
                {
                    UpdateRate = $"{_updatesInLastSecond} Hz";
                    _updatesInLastSecond = 0;
                    _lastRateCalc = now;
                }
            });
        }
    }
}