using System;
using Microsoft.Extensions.Logging;

namespace VsTwitch
{
    public sealed class ILoggerImpl<T> : ILogger<T>
    {
        public event EventHandler<String> OnLog;

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => default!;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception exception,
            Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            OnLog?.Invoke(this, formatter.Invoke(state, exception));
        }
    }
}
