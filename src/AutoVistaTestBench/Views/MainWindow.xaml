using System.Windows;
using System.Windows.Threading;
using AutoVistaTestBench.ViewModels;

namespace AutoVistaTestBench.Views
{
    /// <summary>
    /// Code-behind for MainWindow.
    /// Kept minimal per MVVM: only UI-specific logic lives here
    /// (system clock timer, window initialization).
    /// All business logic belongs in ViewModels.
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly DispatcherTimer _clockTimer;

        public MainWindow(MainViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;

            // System clock display timer (UI-only concern — belongs in code-behind)
            _clockTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _clockTimer.Tick += (s, e) =>
            {
                SystemClockText.Text = DateTime.Now.ToString("yyyy-MM-dd  HH:mm:ss");
            };
            _clockTimer.Start();

            // Set initial clock text immediately
            SystemClockText.Text = DateTime.Now.ToString("yyyy-MM-dd  HH:mm:ss");
        }

        protected override void OnClosed(EventArgs e)
        {
            _clockTimer.Stop();
            base.OnClosed(e);
        }
    }
}