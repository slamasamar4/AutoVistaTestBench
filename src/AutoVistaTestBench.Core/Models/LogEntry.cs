using AutoVistaTestBench.Core.Enums;

namespace AutoVistaTestBench.Core.Models
{
    /// <summary>
    /// Represents a single structured log entry for the test bench.
    /// Structured logging ensures logs are both human-readable and machine-parseable,
    /// enabling post-processing and AI analysis.
    /// </summary>
    public class LogEntry
    {
        /// <summary>Sequential log entry identifier within a session.</summary>
        public long EntryId { get; set; }

        /// <summary>UTC timestamp of the event.</summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>Severity of this log entry.</summary>
        public LogSeverity Severity { get; set; }

        /// <summary>Source component or module generating this entry.</summary>
        public string Source { get; set; } = string.Empty;

        /// <summary>Human-readable message body.</summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>Optional: associated channel ID if this log relates to a specific channel.</summary>
        public string? ChannelId { get; set; }

        /// <summary>Optional: numeric value associated with this event (e.g., fault value).</summary>
        public double? Value { get; set; }

        /// <summary>Optional: session ID for cross-referencing.</summary>
        public Guid? SessionId { get; set; }

        /// <summary>
        /// Formats the log entry as a single-line string suitable for file logging.
        /// Format is compatible with standard log parsers.
        /// </summary>
        public override string ToString() =>
            $"[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Severity,-8}] [{Source,-20}] {Message}" +
            (ChannelId != null ? $" | Channel:{ChannelId}" : "") +
            (Value.HasValue ? $" | Value:{Value:F4}" : "");
    }
}