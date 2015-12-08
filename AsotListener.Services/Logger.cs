namespace AsotListener.Services
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Windows.Foundation.Diagnostics;

    public class Logger : ILogger
    {
        private static Lazy<Logger> lazy = new Lazy<Logger>(() => new Logger());
        LoggingSession loggingSession;
        LoggingChannel loggingChannel;

        public static Logger Instance => lazy.Value;

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

        private void saveLogsToFile()
        {
            // TODO: Implement this
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    saveLogsToFile();
                    loggingSession.Dispose();
                }

                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }

        #endregion
    }
}
