using System.Windows;
using AutoVistaTestBench.Core.Interfaces;
using AutoVistaTestBench.Services.Acquisition;
using AutoVistaTestBench.Services.Ai;
using AutoVistaTestBench.Services.Communication;
using AutoVistaTestBench.Services.Logging;
using AutoVistaTestBench.Simulator;
using AutoVistaTestBench.ViewModels;
using AutoVistaTestBench.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AutoVistaTestBench
{
    /// <summary>
    /// Application entry point and Dependency Injection container configuration.
    /// 
    /// Uses Microsoft.Extensions.DependencyInjection — the same DI container
    /// used in ASP.NET Core, making the pattern familiar to web developers.
    /// 
    /// Service lifetimes:
    /// - Singleton: Services that maintain shared state (simulator, logging, acquisition)
    /// - Transient: ViewModels that don't need to persist (created fresh each time)
    /// </summary>
    public partial class App : Application
    {
        private ServiceProvider? _serviceProvider;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var services = new ServiceCollection();
            ConfigureServices(services);

            _serviceProvider = services.BuildServiceProvider();

            // Set up global exception handling for unhandled UI thread exceptions
            DispatcherUnhandledException += (s, ex) =>
            {
                var logger = _serviceProvider.GetService<ILogger<App>>();
                logger?.LogCritical(ex.Exception, "Unhandled UI thread exception");
                MessageBox.Show(
                    $"An unexpected error occurred:\n\n{ex.Exception.Message}\n\nThe application will attempt to continue.",
                    "AutoVista Test Bench — Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                ex.Handled = true;
            };

            // Show the main window
            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }

        /// <summary>
        /// Configures the DI service container.
        /// Order: Infrastructure → Core Services → ViewModels → Views
        /// </summary>
        private static void ConfigureServices(IServiceCollection services)
        {
            // ── Logging ──────────────────────────────────────────────────────────────
            services.AddLogging(configure =>
            {
                configure.SetMinimumLevel(LogLevel.Debug);
                configure.AddConsole(); // Output to debug console in VS
            });

            // ── Hardware Simulation ───────────────────────────────────────────────
            // Registered as Singleton because there is one physical hardware instance
            services.AddSingleton<IHardwareSimulator, HardwareSimulator>();

            // ── Core Services ─────────────────────────────────────────────────────
            services.AddSingleton<ILoggingService, FileLoggingService>();
            services.AddSingleton<ICanBusService, CanBusService>();
            services.AddSingleton<IDataAcquisitionService, DataAcquisitionService>();
            services.AddSingleton<IAiAnalysisService, AiAnalysisService>();

            // ── ViewModels ────────────────────────────────────────────────────────
            // DashboardViewModel is Singleton because it holds live channel state
            services.AddSingleton<MainViewModel>();
            services.AddSingleton<DashboardViewModel>();
            services.AddSingleton<ChannelMonitorViewModel>();
            services.AddSingleton<LogAnalyzerViewModel>();

            // ── Views ─────────────────────────────────────────────────────────────
            services.AddTransient<MainWindow>();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // Ensure acquisition is stopped and resources are released on exit
            var acquisitionService = _serviceProvider?.GetService<IDataAcquisitionService>();
            if (acquisitionService?.IsAcquiring == true)
                acquisitionService.StopSessionAsync().GetAwaiter().GetResult();

            _serviceProvider?.Dispose();
            base.OnExit(e);
        }
    }
}