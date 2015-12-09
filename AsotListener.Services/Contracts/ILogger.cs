namespace AsotListener.Services.Contracts
{
    using Windows.Foundation.Diagnostics;

    public interface ILogger
    {
        void LogMessage(string message);
        void LogMessage(string message, LoggingLevel loggingLevel);
        void SaveLogsToFile(string fileName);
    }
}
