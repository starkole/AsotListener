namespace AsotListener.Services.Implementations
{
    using System;
    using System.Diagnostics;
    using Contracts;
    using Windows.Foundation.Diagnostics;
    using Windows.Storage;

    /// <summary>
    /// Simple logger
    /// </summary>
    public sealed class Logger : ILogger, IDisposable
    {
        private readonly LoggingSession loggingSession;
        private readonly LoggingChannel loggingChannel;
        private bool isDisposed = false;

        /// <summary>
        /// Creates instance of <see cref="Logger"/>
        /// </summary>
        public Logger()
        {
            loggingSession = new LoggingSession("ASOT Listener");
            loggingChannel = new LoggingChannel($"Task{Environment.CurrentManagedThreadId}Channel");
            loggingSession.AddLoggingChannel(loggingChannel);
            LogMessage($"Logger initialized on channel {loggingChannel.Name}.", LoggingLevel.Information);
        }

        /// <summary>
        /// Logs message with verbose logging level
        /// </summary>
        /// <param name="message"></param>
        public void LogMessage(string message)
        {
            LogMessage(message, LoggingLevel.Verbose);
        }

        /// <summary>
        /// Logs message with provided logging level
        /// </summary>
        /// <param name="message">Message to log</param>
        /// <param name="loggingLevel">Message logging level</param>
        public void LogMessage(string message, LoggingLevel loggingLevel)
        {
            if (loggingLevel >= LoggingLevel.Warning)
            {
                loggingChannel.LogMessage(message, loggingLevel);
            }
#if DEBUG
            Debug.WriteLine($"{loggingLevel}: {message}");
#endif
        }

        /// <summary>
        /// Saves collected logs to file
        /// </summary>
        public void SaveLogsToFile()
        {
            var saveTask = loggingSession.SaveToFileAsync(ApplicationData.Current.LocalFolder, "log_" + loggingChannel.Name + ".etl").AsTask();
            saveTask.ConfigureAwait(false);
            saveTask.Wait();
        }

        /// <summary>
        /// Closes logging sessions and releases used resources
        /// </summary>
        public void Dispose()
        {
            if (!isDisposed)
            {
                loggingSession.RemoveLoggingChannel(loggingChannel);
                loggingChannel.Dispose();
                loggingSession.Dispose();
                isDisposed = true;
            }
        }
    }
}
