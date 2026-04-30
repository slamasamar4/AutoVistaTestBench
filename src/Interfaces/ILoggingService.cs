using AutoVistaTestBench.Core.Enums;
using AutoVistaTestBench.Core.Models;

namespace AutoVistaTestBench.Core.Interfaces
{
    /// <summary>
    /// Provides structured logging to persistent storage (file, database, etc.).
    /// Designed for automotive test bench logging requirements:
    /// timestamped, session-scoped, severity-filtered log files.
    /// </summary>
    public interface ILoggingService
    {
        /// <summary>Starts a new log file for the given session.</summary>
        Task OpenSessionLogAsync(TestSession session);

        /// <summary>Closes and flushes the current session log.</summary>
        Task CloseSessionLogAsync();

        /// <summary>Writes a structured log entry to the active log file.</summary>
        Task WriteAsync(LogSeverity severity, string source, string message,
            string? channelId = null, double? value = null);

        /// <summary>Returns all log entries written during the current session.</summary>
        IReadOnlyList<LogEntry> GetSessionEntries();

        /// <summary>True if a session log is currently open and accepting writes.</summary>
        bool IsOpen { get; }
    }
}