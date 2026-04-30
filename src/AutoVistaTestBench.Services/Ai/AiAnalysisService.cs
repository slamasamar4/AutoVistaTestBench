using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using AutoVistaTestBench.Core.Enums;
using AutoVistaTestBench.Core.Interfaces;
using AutoVistaTestBench.Core.Models;
using Microsoft.Extensions.Logging;

namespace AutoVistaTestBench.Services.Ai
{
    /// <summary>
    /// Provides AI-powered log analysis by sending structured log data to the Anthropic Claude API.
    /// 
    /// Configuration:
    /// Set the AUTOVISTA_AI_API_KEY environment variable with your Anthropic API key.
    /// Alternatively, set it in appsettings.json or the app configuration.
    /// 
    /// API Reference: https://docs.anthropic.com/claude/reference/messages_post
    /// 
    /// Note: This service degrades gracefully — if no API key is configured,
    /// it returns a placeholder message instead of throwing.
    /// </summary>
    public class AiAnalysisService : IAiAnalysisService, IDisposable
    {
        private readonly ILogger<AiAnalysisService> _logger;
        private readonly HttpClient _httpClient;
        private readonly string? _apiKey;

        private const string ApiEndpoint = "https://api.anthropic.com/v1/messages";
        private const string ModelId = "claude-opus-4-5";
        private const string ApiVersion = "2023-06-01";

        public bool IsConfigured => !string.IsNullOrEmpty(_apiKey);

        public AiAnalysisService(ILogger<AiAnalysisService> logger)
        {
            _logger = logger;

            // API key loaded from environment variable (safe, not hardcoded)
            _apiKey = Environment.GetEnvironmentVariable("AUTOVISTA_AI_API_KEY");

            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };

            if (!string.IsNullOrEmpty(_apiKey))
            {
                _httpClient.DefaultRequestHeaders.Add("x-api-key", _apiKey);
                _httpClient.DefaultRequestHeaders.Add("anthropic-version", ApiVersion);
                _logger.LogInformation("AI Analysis Service configured with API key");
            }
            else
            {
                _logger.LogWarning(
                    "AI Analysis Service: No API key found in AUTOVISTA_AI_API_KEY environment variable. " +
                    "AI features will return placeholder responses.");
            }
        }

        /// <summary>
        /// Analyzes a set of log entries and returns a structured AI-generated report.
        /// Sends the last N log entries as a formatted prompt to the Claude API.
        /// </summary>
        public async Task<string> AnalyzeLogsAsync(IReadOnlyList<LogEntry> entries, string sessionSummary)
        {
            if (!IsConfigured)
                return GetPlaceholderResponse("log analysis");

            // Only send last 50 entries to stay within token limits and keep analysis focused
            var recentEntries = entries
                .OrderByDescending(e => e.Timestamp)
                .Take(50)
                .OrderBy(e => e.Timestamp)
                .ToList();

            var logText = new StringBuilder();
            logText.AppendLine($"=== TEST SESSION SUMMARY ===\n{sessionSummary}\n");
            logText.AppendLine("=== RECENT LOG ENTRIES ===");
            foreach (var entry in recentEntries)
                logText.AppendLine(entry.ToString());

            var prompt = $"""
                You are an automotive ECU test bench diagnostic AI assistant.
                Analyze the following test session log from an automotive ECU test bench.
                
                Your analysis should:
                1. Identify any fault patterns or anomalies
                2. Assess the severity and potential root causes
                3. Suggest corrective actions for the test engineer
                4. Note any safety-relevant findings (ISO 26262 perspective)
                5. Rate the overall test session health: PASS / PASS WITH WARNINGS / FAIL
                
                Keep your response concise (max 400 words) and engineer-focused.
                
                {logText}
                """;

            return await CallClaudeApiAsync(prompt);
        }

        /// <summary>
        /// Analyzes a specific anomaly and returns a root cause suggestion.
        /// </summary>
        public async Task<string> AnalyzeAnomalyAsync(AnomalyReport anomaly, string channelContext)
        {
            if (!IsConfigured)
                return GetPlaceholderResponse("anomaly analysis");

            var prompt = $"""
                You are an automotive ECU diagnostic specialist.
                Analyze the following anomaly detected on a test bench:
                
                Channel: {anomaly.ChannelName} (ID: {anomaly.ChannelId})
                Detected at: {anomaly.DetectedAt:HH:mm:ss}
                Measured value: {anomaly.TriggerValue:F3}
                Fault threshold: {anomaly.ThresholdValue:F3}
                Description: {anomaly.Description}
                Channel context: {channelContext}
                
                Provide:
                1. Most likely root cause (2-3 sentences)
                2. Recommended diagnostic step
                3. Is this likely a sensor fault, wiring issue, or real ECU fault?
                
                Be concise and actionable (max 150 words).
                """;

            return await CallClaudeApiAsync(prompt);
        }

        /// <summary>
        /// Makes the actual HTTP request to the Anthropic Messages API.
        /// Uses the Messages API format (not legacy Text Completion).
        /// </summary>
        private async Task<string> CallClaudeApiAsync(string userPrompt)
        {
            try
            {
                var requestBody = new
                {
                    model = ModelId,
                    max_tokens = 1024,
                    messages = new[]
                    {
                        new { role = "user", content = userPrompt }
                    }
                };

                var json = JsonSerializer.Serialize(requestBody);
                using var content = new StringContent(json, Encoding.UTF8, "application/json");

                _logger.LogDebug("Sending request to Claude API...");
                var response = await _httpClient.PostAsync(ApiEndpoint, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Claude API error: {Status} — {Body}",
                        response.StatusCode, errorBody);
                    return $"[AI Analysis Error: HTTP {(int)response.StatusCode} — {response.ReasonPhrase}]";
                }

                var responseJson = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseJson);

                // Extract text from the first content block in the response
                var textContent = doc.RootElement
                    .GetProperty("content")[0]
                    .GetProperty("text")
                    .GetString();

                return textContent ?? "[AI returned empty response]";
            }
            catch (TaskCanceledException)
            {
                _logger.LogWarning("Claude API request timed out");
                return "[AI Analysis: Request timed out (30s). Check your network connection.]";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Claude API");
                return $"[AI Analysis Error: {ex.Message}]";
            }
        }

        private static string GetPlaceholderResponse(string analysisType) =>
            $"[AI {analysisType} not available — Set the AUTOVISTA_AI_API_KEY environment variable " +
            $"with your Anthropic API key to enable AI-powered analysis. " +
            $"Visit https://console.anthropic.com to obtain an API key.]";

        public void Dispose()
        {
            _httpClient.Dispose();
        }
    }
}