namespace AsotListener.Services.Implementations
{
    using System;
    using System.Diagnostics;
    using Contracts;
    using Windows.Foundation.Diagnostics;

    public class Logger : ILogger
    {
        private static Lazy<ILogger> lazy = new Lazy<ILogger>(() => new Logger());
        LoggingSession loggingSession;
        LoggingChannel loggingChannel;

        public static ILogger Instance => lazy.Value;

        private Logger()
        {
            loggingSession = new LoggingSession("ASOT Listener");
            loggingChannel = new LoggingChannel("Common logging channel");
            loggingSession.AddLoggingChannel(loggingChannel);
        }

        public void LogMessage(string message)
        {
            this.LogMessage(message, LoggingLevel.Verbose);
        }

        public void LogMessage(string message, LoggingLevel loggingLevel)
        {
            loggingChannel.LogMessage(message, loggingLevel);
#if DEBUG
            Debug.WriteLine($"{loggingLevel}: {message}");
#endif
        }

        public void SaveLogsToFile(string filename)
        {
            // TODO: Implement this
        }        
    }
}
