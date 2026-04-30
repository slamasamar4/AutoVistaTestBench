using System.Collections.Concurrent;
using System.Text;
using AutoVistaTestBench.Core.Enums;
using AutoVistaTestBench.Core.Interfaces;
using AutoVistaTestBench.Core.Models;
using Microsoft.Extensions.Logging;

namespace AutoVistaTestBench.Services.Logging
{
    /// <summary>
    /// Provides structured file-based logging for test sessions.
    /// 
    /// Design decisions:
    /// - Uses a ConcurrentQueue for thread-safe log entry buffering
    /// - Dedicated background writer thread prevents I/O from blocking the acquisition thread
    /// - Log format is tab-separated for easy import into Excel/MATLAB
    /// - Log directory: %APPDATA%\AutoVistaTestBench\Logs\
    /// 
    /// In production: Consider extending with SQLite or InfluxDB for time-series storage.
    /// </summary>
    public class FileLoggingService : ILoggingService, IDisposable
    {
        private readonly ILogger<FileLoggingService> _logger;
        private readonly string _logDirectory;
        private readonly ConcurrentQueue<LogEntry> _writeQueue = new();
        private readonly List<LogEntry> _sessionEntries = new();
        private readonly object _entriesLock = new();

        private StreamWriter? _writer;
        private Thread? _writerThread;
        private volatile bool _isOpen;
        private volatile bool _stopWriter;
        private long _entryCounter;
        private bool _disposed;

        public bool IsOpen => _isOpen;

        public FileLoggingService(ILogger<FileLoggingService> logger)
        {
            _logger = logger;

            // Store logs in user's AppData folder (appropriate for desktop apps)
            _logDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "AutoVistaTestBench", "Logs");

            Directory.CreateDirectory(_logDirectory);
        }

        /// <summary>
        /// Opens a new log file for the given test session.
        /// File name format: YYYY-MM-DD_HH-mm-ss_SessionName.log
        /// </summary>
        public async Task OpenSessionLogAsync(TestSession session)
        {
            if (_isOpen)
            {
                await CloseSessionLogAsync();
            }

            string fileName = $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss}_{SanitizeFileName(session.SessionName)}.log";
            string filePath = Path.Combine(_logDirectory, fileName);

            session.LogFilePath = filePath;

            try
            {
                _writer = new StreamWriter(filePath, append: false, Encoding.UTF8)
                {
                    AutoFlush = false // Manual flush for performance; flushed by writer thread
                };

                // Write file header compatible with standard log viewers
                await _writer.WriteLineAsync(
                    "# AutoVista ECU Test Bench — Session Log\n" +
                    $"# Session: {session.SessionName}\n" +
                    $"# Session ID: {session.SessionId}\n" +
                    $"# Operator: {session.OperatorName}\n" +
                    $"# Vehicle: {session.VehicleId}\n" +
                    $"# Start Time: {session.StartTime:yyyy-MM-dd HH:mm:ss.fff} UTC\n" +
                    $"# Format: [Timestamp] [Severity] [Source] Message | Channel:... | Value:...\n" +
                    new string('-', 120));

                await _writer.FlushAsync();

                lock (_entriesLock)
                {
                    _sessionEntries.Clear();
                    _entryCounter = 0;
                }

                _stopWriter = false;
                _isOpen = true;

                // Start background writer thread
                _writerThread = new Thread(WriterLoop)
                {
                    Name = "LogWriterThread",
                    IsBackground = true,
                    Priority = ThreadPriority.BelowNormal  // I/O thread gets lower priority
                };
                _writerThread.Start();

                _logger.LogInformation("Session log opened: {FilePath}", filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to open session log file: {FilePath}", filePath);
                throw;
            }
        }

        /// <summary>
        /// Signals the writer thread to stop, flushes remaining entries, and closes the file.
        /// </summary>
        public async Task CloseSessionLogAsync()
        {
            if (!_isOpen) return;

            _isOpen = false;
            _stopWriter = true;

            // Wait for the writer thread to flush remaining entries
            _writerThread?.Join(timeout: TimeSpan.FromSeconds(5));

            if (_writer != null)
            {
                await _writer.WriteLineAsync(
                    $"\n{new string('-', 120)}\n# Session End — Total Entries: {_entryCounter}");
                await _writer.FlushAsync();
                await _writer.DisposeAsync();
                _writer = null;
            }

            _logger.LogInformation("Session log closed — {EntryCount} entries written", _entryCounter);
        }

        /// <summary>
        /// Enqueues a log entry for asynchronous file writing.
        /// This method returns immediately — actual file I/O happens on the writer thread.
        /// </summary>
        public Task WriteAsync(LogSeverity severity, string source, string message,
            string? channelId = null, double? value = null)
        {
            var entry = new LogEntry
            {
                EntryId = Interlocked.Increment(ref _entryCounter),
                Timestamp = DateTime.UtcNow,
                Severity = severity,
                Source = source,
                Message = message,
                ChannelId = channelId,
                Value = value
            };

            _writeQueue.Enqueue(entry);

            lock (_entriesLock)
            {
                _sessionEntries.Add(entry);
                // Keep in-memory list at max 10,000 entries (prevent unbounded growth)
                if (_sessionEntries.Count > 10000)
                    _sessionEntries.RemoveAt(0);
            }

            return Task.CompletedTask;
        }

        public IReadOnlyList<LogEntry> GetSessionEntries()
        {
            lock (_entriesLock)
            {
                return _sessionEntries.ToList().AsReadOnly();
            }
        }

        /// <summary>
        /// Background writer loop that drains the write queue to the file.
        /// Batches writes and flushes periodically to balance performance vs. data safety.
        /// </summary>
        private void WriterLoop()
        {
            int flushCounter = 0;

            while (!_stopWriter || !_writeQueue.IsEmpty)
            {
                int written = 0;

                while (_writeQueue.TryDequeue(out var entry))
                {
                    try
                    {
                        _writer?.WriteLine(entry.ToString());
                        written++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error writing log entry to file");
                    }
                }

                // Flush to disk every 50 writes or every 500ms
                if (written > 0)
                {
                    flushCounter += written;
                    if (flushCounter >= 50)
                    {
                        _writer?.Flush();
                        flushCounter = 0;
                    }
                }

                if (!_stopWriter)
                    Thread.Sleep(100);
            }

            // Final flush before thread exits
            _writer?.Flush();
        }

        private static string SanitizeFileName(string name) =>
            string.Concat(name.Split(Path.GetInvalidFileNameChars())).Replace(' ', '_');

        public void Dispose()
        {
            if (_disposed) return;
            CloseSessionLogAsync().GetAwaiter().GetResult();
            _disposed = true;
        }
    }
}