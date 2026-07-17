using System.Text;

namespace MesWEB.Logging
{
    public class FileLoggerProvider : ILoggerProvider
    {
        private readonly string _logsFolder;
        private readonly LogLevel _minLevel;
        private readonly object _sync = new object();

        public FileLoggerProvider(string logsFolder, LogLevel minLevel = LogLevel.Information)
        {
            _logsFolder = string.IsNullOrWhiteSpace(logsFolder) ? Path.Combine(Directory.GetCurrentDirectory(), "logs") : logsFolder;
            _minLevel = minLevel;
            try
            {
                Directory.CreateDirectory(_logsFolder);
            }
            catch
            {
                // ignore
            }
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new FileLogger(this, categoryName);
        }

        public void Dispose()
        {
        }

        private class FileLogger : ILogger
        {
            private readonly FileLoggerProvider _provider;
            private readonly string _category;

            public FileLogger(FileLoggerProvider provider, string category)
            {
                _provider = provider;
                _category = category;
            }

            public IDisposable? BeginScope<TState>(TState state) where TState : notnull
            {
                return null;
            }

            public bool IsEnabled(LogLevel logLevel)
            {
                return logLevel >= _provider._minLevel;
            }

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                if (!IsEnabled(logLevel)) return;

                var message = formatter(state, exception);
                var ts = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                var ex = exception != null ? ("\n" + exception.ToString()) : string.Empty;
                var line = new StringBuilder();
                line.Append(ts);
                line.Append(' ');
                line.Append(logLevel.ToString().PadRight(7));
                line.Append(' ');
                line.Append('[').Append(_category).Append(']').Append(' ');
                line.Append(message);
                if (!string.IsNullOrEmpty(ex)) line.Append(ex);
                line.AppendLine();

                try
                {
                    var fileName = Path.Combine(_provider._logsFolder, $"mesweb-{DateTime.Now:yyyyMMdd}.log");
                    lock (_provider._sync)
                    {
                        File.AppendAllText(fileName, line.ToString(), Encoding.UTF8);
                    }
                }
                catch
                {
                    // swallow logging errors to avoid crashing app
                }
            }
        }
    }
}
