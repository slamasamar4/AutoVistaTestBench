using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace AutoVistaTestBench.ViewModels
{
    /// <summary>
    /// ViewModel for the real-time channel monitor view.
    /// </summary>
    public class ChannelMonitorViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<ChannelViewModel> _channels = new();
        private ChannelViewModel? _selectedChannel;
        private long _totalUpdateCount;
        private string _updateRate = "0 Hz";

        public ObservableCollection<ChannelViewModel> Channels
        {
            get => _channels;
            set
            {
                _channels = value;
                OnPropertyChanged();
            }
        }

        public ChannelViewModel? SelectedChannel
        {
            get => _selectedChannel;
            set
            {
                _selectedChannel = value;
                OnPropertyChanged();
            }
        }

        public long TotalUpdateCount
        {
            get => _totalUpdateCount;
            set
            {
                _totalUpdateCount = value;
                OnPropertyChanged();
            }
        }

        public string UpdateRate
        {
            get => _updateRate;
            set
            {
                _updateRate = value;
                OnPropertyChanged();
            }
        }

        public ChannelMonitorViewModel()
        {
            InitializeSampleChannels();
        }

        private void InitializeSampleChannels()
        {
            var now = DateTime.Now.ToString("HH:mm:ss");
            
            var channel1 = new ChannelViewModel();
            channel1.Id = "CH-001";
            channel1.Name = "Engine Temperature";
            channel1.EcuModuleId = "ECU_01";
            channel1.Type = "Temperature";
            channel1.ValueDisplay = "95.5";
            channel1.Unit = "°C";
            channel1.StatusLabel = "NORMAL";
            channel1.NormalizedValue = 0.55;
            channel1.LastUpdatedDisplay = now;
            Channels.Add(channel1);

            var channel2 = new ChannelViewModel();
            channel2.Id = "CH-002";
            channel2.Name = "Battery Voltage";
            channel2.EcuModuleId = "ECU_03";
            channel2.Type = "Voltage";
            channel2.ValueDisplay = "12.8";
            channel2.Unit = "V";
            channel2.StatusLabel = "WARNING";
            channel2.NormalizedValue = 0.75;
            channel2.LastUpdatedDisplay = now;
            Channels.Add(channel2);

            var channel3 = new ChannelViewModel();
            channel3.Id = "CH-003";
            channel3.Name = "Coolant Pressure";
            channel3.EcuModuleId = "ECU_01";
            channel3.Type = "Pressure";
            channel3.ValueDisplay = "45.2";
            channel3.Unit = "psi";
            channel3.StatusLabel = "NORMAL";
            channel3.NormalizedValue = 0.32;
            channel3.LastUpdatedDisplay = now;
            Channels.Add(channel3);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// ViewModel for an individual channel in the channel monitor.
    /// </summary>
    public class ChannelViewModel : INotifyPropertyChanged
    {
        private string _id = string.Empty;
        private string _name = string.Empty;
        private string _ecuModuleId = string.Empty;
        private string _type = string.Empty;
        private string _valueDisplay = "0.00";
        private string _unit = string.Empty;
        private string _statusLabel = "IDLE";
        private double _normalizedValue;
        private string _lastUpdatedDisplay = string.Empty;
        private Brush _statusColor = Brushes.Gray;
        private Brush _valueColor = Brushes.White;
        private Brush _rowBackground = Brushes.Transparent;

        public string Id
        {
            get => _id;
            set
            {
                _id = value;
                OnPropertyChanged();
            }
        }

        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged();
            }
        }

        public string EcuModuleId
        {
            get => _ecuModuleId;
            set
            {
                _ecuModuleId = value;
                OnPropertyChanged();
            }
        }

        public string Type
        {
            get => _type;
            set
            {
                _type = value;
                OnPropertyChanged();
            }
        }

        public string ValueDisplay
        {
            get => _valueDisplay;
            set
            {
                _valueDisplay = value;
                OnPropertyChanged();
            }
        }

        public string Unit
        {
            get => _unit;
            set
            {
                _unit = value;
                OnPropertyChanged();
            }
        }

        public string StatusLabel
        {
            get => _statusLabel;
            set
            {
                _statusLabel = value;
                OnPropertyChanged();
            }
        }

        public double NormalizedValue
        {
            get => _normalizedValue;
            set
            {
                _normalizedValue = value;
                OnPropertyChanged();
            }
        }

        public string LastUpdatedDisplay
        {
            get => _lastUpdatedDisplay;
            set
            {
                _lastUpdatedDisplay = value;
                OnPropertyChanged();
            }
        }

        public Brush StatusColor
        {
            get => _statusColor;
            set
            {
                _statusColor = value;
                OnPropertyChanged();
            }
        }

        public Brush ValueColor
        {
            get => _valueColor;
            set
            {
                _valueColor = value;
                OnPropertyChanged();
            }
        }

        public Brush RowBackground
        {
            get => _rowBackground;
            set
            {
                _rowBackground = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void NotifyUpdate()
        {
            LastUpdatedDisplay = DateTime.Now.ToString("HH:mm:ss");
            var random = new Random();
            NormalizedValue = random.NextDouble();
            ValueDisplay = (NormalizedValue * 100).ToString("F1");

            if (NormalizedValue > 0.8)
            {
                StatusLabel = "CRITICAL";
                StatusColor = new SolidColorBrush(Colors.Red);
                ValueColor = new SolidColorBrush(Colors.Red);
            }
            else if (NormalizedValue > 0.6)
            {
                StatusLabel = "WARNING";
                StatusColor = new SolidColorBrush(Colors.Orange);
                ValueColor = new SolidColorBrush(Colors.Orange);
            }
            else
            {
                StatusLabel = "NORMAL";
                StatusColor = new SolidColorBrush(Colors.Green);
                ValueColor = Brushes.White;
            }
        }
    }
}