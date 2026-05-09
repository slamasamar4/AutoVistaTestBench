using System.Windows;
using AutoVistaTestBench.ViewModels;
using AutoVistaTestBench.Views;

namespace AutoVistaTestBench
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // CRITICAL: Set shutdown mode to explicit so the app doesn't close prematurely
            this.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            // Add global exception handling to catch any errors
            this.DispatcherUnhandledException += (s, args) =>
            {
                MessageBox.Show($"Error: {args.Exception.Message}\n\n{args.Exception.StackTrace}",
                    "Application Error", MessageBoxButton.OK, MessageBoxImage.Error);
                args.Handled = true;
            };

            try
            {
                // Create main window
                var mainViewModel = new MainViewModel();
                var mainWindow = new MainWindow(mainViewModel);
                
                // Explicitly set as the application's main window
                this.MainWindow = mainWindow;
                
                // Now switch back to normal shutdown mode
                this.ShutdownMode = ShutdownMode.OnMainWindowClose;
                
                // Show the window
                mainWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Startup failed: {ex.Message}\n\n{ex.StackTrace}",
                    "Critical Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}