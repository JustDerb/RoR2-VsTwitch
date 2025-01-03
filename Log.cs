
using BepInEx.Logging;
using Microsoft.Extensions.Logging;
using System;

namespace VsTwitch
{
    internal static class Log
    {
        private static ManualLogSource _logSource;

        internal static void Init(ManualLogSource logSource)
        {
            _logSource = logSource;
        }

        internal static ILoggerFactory CreateLoggerFactory(Func<string?, string?, Microsoft.Extensions.Logging.LogLevel, bool> filter)
        {
            ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                    .AddFilter(filter)
                    .AddConsole();
            });
            return loggerFactory;
        }

        internal static void Debug(object data) => _logSource.LogDebug(data);
        internal static void Error(object data) => _logSource.LogError(data);

        internal static void Exception(System.Exception data) => _logSource.LogError(data);
        internal static void Fatal(object data) => _logSource.LogFatal(data);
        internal static void Info(object data) => _logSource.LogInfo(data);
        internal static void Message(object data) => _logSource.LogMessage(data);
        internal static void Warning(object data) => _logSource.LogWarning(data);
    }
}