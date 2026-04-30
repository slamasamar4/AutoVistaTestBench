namespace AutoVistaTestBench.Core.Models
{
    /// <summary>
    /// Represents a complete test session with metadata, timing, and summary statistics.
    /// Equivalent to a "test run" record in professional test automation frameworks
    /// like NI TestStand or ETAS INCA.
    /// </summary>
    public class TestSession
    {
        /// <summary>Unique session identifier (GUID-based for traceability).</summary>
        public Guid SessionId { get; set; } = Guid.NewGuid();

        /// <summary>Human-readable session name set by the operator.</summary>
        public string SessionName { get; set; } = string.Empty;

        /// <summary>Name of the test engineer running this session.</summary>
        public string OperatorName { get; set; } = string.Empty;

        /// <summary>Vehicle or component under test identifier.</summary>
        public string VehicleId { get; set; } = string.Empty;

        /// <summary>UTC timestamp when the session started.</summary>
        public DateTime StartTime { get; set; }

        /// <summary>UTC timestamp when the session ended. Null if still running.</summary>
        public DateTime? EndTime { get; set; }

        /// <summary>Session duration. Returns elapsed time if still running.</summary>
        public TimeSpan Duration =>
            EndTime.HasValue
                ? EndTime.Value - StartTime
                : DateTime.UtcNow - StartTime;

        /// <summary>Total number of data samples acquired during this session.</summary>
        public long TotalSamples { get; set; }

        /// <summary>Total number of fault events detected during this session.</summary>
        public int FaultCount { get; set; }

        /// <summary>Total number of warning events during this session.</summary>
        public int WarningCount { get; set; }

        /// <summary>Whether the session completed without critical faults.</summary>
        public bool Passed => FaultCount == 0;

        /// <summary>Absolute path to the log file for this session.</summary>
        public string LogFilePath { get; set; } = string.Empty;

        /// <summary>List of anomaly reports generated during the session.</summary>
        public List<AnomalyReport> AnomalyReports { get; set; } = new();

        /// <summary>Returns a formatted summary string for reporting.</summary>
        public string GetSummary() =>
            $"Session: {SessionName} | ID: {SessionId} | Operator: {OperatorName} | " +
            $"Duration: {Duration:hh\\:mm\\:ss} | Samples: {TotalSamples} | " +
            $"Faults: {FaultCount} | Warnings: {WarningCount} | Result: {(Passed ? "PASS" : "FAIL")}";
    }
}