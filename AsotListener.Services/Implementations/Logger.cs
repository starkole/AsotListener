namespace AsotListener.Services.Implementations
{
    using System;
    using System.Diagnostics;
    using Contracts;
    using Windows.Foundation.Diagnostics;

    public sealed class Logger : ILogger, IDisposable
    {
        LoggingSession loggingSession;
        LoggingChannel loggingChannel;

        public Logger()
        {
            loggingSession = new LoggingSession("ASOT Listener");
            loggingChannel = new LoggingChannel("Common logging channel");
            loggingSession.AddLoggingChannel(loggingChannel);
            LogMessage("Logger initialized.");
        }

        public void LogMessage(string message)
        {
            LogMessage(message, LoggingLevel.Verbose);
        }

        public void LogMessage(string message, LoggingLevel loggingLevel)
        {
            loggingChannel.LogMessage(message, loggingLevel);
#if DEBUG
            Debug.WriteLine($"{loggingLevel}: {message}");
#endif
        }

        public void SaveLogsToFile()
        {
            // TODO: Implement this
        }

        public void Dispose()
        {
            loggingSession.RemoveLoggingChannel(loggingChannel);
            loggingChannel.Dispose();
            loggingSession.Dispose();
        }
    }
}
