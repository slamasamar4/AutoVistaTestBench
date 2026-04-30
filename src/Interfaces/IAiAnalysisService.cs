using AutoVistaTestBench.Core.Models;

namespace AutoVistaTestBench.Core.Interfaces
{
    /// <summary>
    /// Provides AI-powered log analysis and anomaly root-cause suggestions.
    /// Sends structured log data to an LLM API (e.g., Anthropic Claude, OpenAI GPT-4)
    /// and returns actionable insights for the test engineer.
    /// </summary>
    public interface IAiAnalysisService
    {
        /// <summary>True if the AI service is configured and reachable.</summary>
        bool IsConfigured { get; }

        /// <summary>
        /// Analyzes a list of log entries and returns a structured AI analysis report.
        /// </summary>
        /// <param name="entries">Recent log entries to analyze.</param>
        /// <param name="sessionSummary">Summary context about the test session.</param>
        Task<string> AnalyzeLogsAsync(IReadOnlyList<LogEntry> entries, string sessionSummary);

        /// <summary>
        /// Analyzes a specific anomaly report and returns a root cause suggestion.
        /// </summary>
        Task<string> AnalyzeAnomalyAsync(AnomalyReport anomaly, string channelContext);
    }
}