using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AutoVistaTestBench.Infrastructure
{
    /// <summary>
    /// Base class for all ViewModels.
    /// Implements INotifyPropertyChanged for WPF data binding.
    /// 
    /// The [CallerMemberName] attribute automatically injects the calling
    /// property name at compile time — no need to pass "nameof(Property)" manually.
    /// This is the standard WPF MVVM pattern.
    /// </summary>
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Sets a backing field and raises PropertyChanged if the value changed.
        /// Returns true if the value was actually changed.
        /// </summary>
        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        /// <summary>Raises PropertyChanged for the specified property name.</summary>
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Safely raises PropertyChanged on the WPF Dispatcher (UI thread).
        /// Use this when updating properties from background threads.
        /// </summary>
        protected void OnPropertyChangedOnDispatcher(string propertyName)
        {
            System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)));
        }
    }
}