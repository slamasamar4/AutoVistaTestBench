namespace AutoVistaTestBench.Core.Enums
{
    /// <summary>
    /// Log severity levels aligned with automotive diagnostics logging conventions.
    /// Mapped loosely to ISO 26262 severity categories for traceability.
    /// </summary>
    public enum LogSeverity
    {
        /// <summary>Diagnostic/trace information, verbose output.</summary>
        Debug,

        /// <summary>Normal operational information.</summary>
        Info,

        /// <summary>Non-critical anomaly; system can continue operating.</summary>
        Warning,

        /// <summary>Critical fault; test may be compromised.</summary>
        Error,

        /// <summary>Safety-critical fault; immediate stop required.</summary>
        Critical
    }
}