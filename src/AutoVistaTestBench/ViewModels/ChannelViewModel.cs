using System.Collections.ObjectModel;
using System.Windows.Media;
using AutoVistaTestBench.Core.Enums;
using AutoVistaTestBench.Core.Models;
using AutoVistaTestBench.Infrastructure;

namespace AutoVistaTestBench.ViewModels
{
    /// <summary>
    /// ViewModel wrapper for a single SensorChannel.
    /// Provides UI-ready properties (Colors, formatted strings, observable collections)
    /// for binding in the channel monitor and dashboard views.
    /// 
    /// This class bridges the domain model (SensorChannel) and the UI,
    /// following the MVVM principle that Views only bind to ViewModels.
    /// </summary>
    public class ChannelViewModel : ViewModelBase
    {
        private SensorChannel _channel;

        public ChannelViewModel(SensorChannel channel)
        {
            _channel = channel;
        }

        // ── Forwarded Model Properties ────────────────────────────────────────────

        public string Id => _channel.Id;
        public string Name => _channel.Name;
        public string Unit => _channel.Unit;
        public string EcuModuleId => _channel.EcuModuleId;
        public SensorType Type => _channel.Type;

        public double CurrentValue
        {
            get => _channel.CurrentValue;
        }

        public ChannelStatus Status => _channel.Status;
        public double NormalizedValue => _channel.NormalizedValue;

        // ── UI-Ready Properties ───────────────────────────────────────────────────

        /// <summary>Formatted display string for the current value.</summary>
        public string ValueDisplay => $"{_channel.CurrentValue:F2} {_channel.Unit}";

        /// <summary>
        /// Status color for the channel indicator LED.
        /// Maps ChannelStatus enum to WPF SolidColorBrush.
        /// </summary>
        public SolidColorBrush StatusColor => _channel.Status switch
        {
            ChannelStatus.Active => new SolidColorBrush(Color.FromRgb(0x2E, 0xCC, 0x71)),  // Green
            ChannelStatus.Warning => new SolidColorBrush(Color.FromRgb(0xF3, 0x9C, 0x12)), // Amber
            ChannelStatus.Fault => new SolidColorBrush(Color.FromRgb(0xE7, 0x4C, 0x3C)),   // Red
            ChannelStatus.Idle => new SolidColorBrush(Color.FromRgb(0x95, 0xA5, 0xA6)),    // Gray
            ChannelStatus.Disabled => new SolidColorBrush(Color.FromRgb(0x34, 0x49, 0x5E)),// Dark gray
            _ => Brushes.Gray
        };

        /// <summary>
        /// Background color for the channel row in the monitor list.
        /// </summary>
        public SolidColorBrush RowBackground => _channel.Status switch
        {
            ChannelStatus.Fault => new SolidColorBrush(Color.FromArgb(40, 0xE7, 0x4C, 0x3C)),
            ChannelStatus.Warning => new SolidColorBrush(Color.FromArgb(30, 0xF3, 0x9C, 0x12)),
            _ => new SolidColorBrush(Colors.Transparent)
        };

        /// <summary>Text color matching the status for value display.</summary>
        public SolidColorBrush ValueColor => _channel.Status switch
        {
            ChannelStatus.Fault => new SolidColorBrush(Color.FromRgb(0xE7, 0x4C, 0x3C)),
            ChannelStatus.Warning => new SolidColorBrush(Color.FromRgb(0xF3, 0x9C, 0x12)),
            _ => new SolidColorBrush(Color.FromRgb(0xEC, 0xF0, 0xF1))
        };

        /// <summary>Status label string for display.</summary>
        public string StatusLabel => _channel.Status.ToString().ToUpperInvariant();

        /// <summary>Timestamp of last update formatted for display.</summary>
        public string LastUpdatedDisplay =>
            _channel.LastUpdated == default
                ? "--"
                : _channel.LastUpdated.ToLocalTime().ToString("HH:mm:ss.fff");

        /// <summary>
        /// Sparkline data points for the mini chart (normalized 0–1 values).
        /// Converts the channel's value history queue to a bindable list.
        /// </summary>
        public IReadOnlyList<double> SparklinePoints =>
            _channel.ValueHistory.ToList().AsReadOnly();

        /// <summary>
        /// Called by the parent ViewModel when the channel receives a new value.
        /// Raises PropertyChanged for all UI-bound properties.
        /// </summary>
        public void NotifyUpdate()
        {
            OnPropertyChanged(nameof(CurrentValue));
            OnPropertyChanged(nameof(ValueDisplay));
            OnPropertyChanged(nameof(Status));
            OnPropertyChanged(nameof(StatusColor));
            OnPropertyChanged(nameof(RowBackground));
            OnPropertyChanged(nameof(ValueColor));
            OnPropertyChanged(nameof(StatusLabel));
            OnPropertyChanged(nameof(LastUpdatedDisplay));
            OnPropertyChanged(nameof(NormalizedValue));
            OnPropertyChanged(nameof(SparklinePoints));
        }

        /// <summary>Updates the underlying model reference.</summary>
        public void UpdateModel(SensorChannel channel)
        {
            _channel = channel;
            NotifyUpdate();
        }
    }
}