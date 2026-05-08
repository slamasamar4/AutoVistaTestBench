using System.Windows;
using AutoVistaTestBench.ViewModels;
using AutoVistaTestBench.Views;

namespace AutoVistaTestBench
{
    /// <summary>
    /// Application entry point
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Add global exception handling
            DispatcherUnhandledException += (s, args) =>
            {
                MessageBox.Show(
                    $"An unexpected error occurred:\n\n{args.Exception.Message}\n\n{args.Exception.StackTrace}",
                    "AutoVista Test Bench — Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                args.Handled = true;
            };

            // Create and show main window with ViewModel
            var mainViewModel = new MainViewModel();
            var mainWindow = new MainWindow(mainViewModel);
            mainWindow.Show();
        }
    }
}