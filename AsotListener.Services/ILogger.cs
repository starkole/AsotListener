namespace AsotListener.Services
{
    using System;
    using Windows.Foundation.Diagnostics;

    public interface ILogger: IDisposable
    {
        void LogMessage(string message);
        void LogMessage(string message, LoggingLevel loggingLevel);
    }
}
