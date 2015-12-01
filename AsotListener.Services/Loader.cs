namespace AsotListener.Services
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Models;
    using Windows.Foundation.Diagnostics;

    public sealed class Loader : ILoader
    {
        private const string MAIN_URL = "http://asotarchive.org";
        private const string FALLBACK_FILENAME = "file";
        private const string FILE_EXTENSION = ".mp3";
        private const int CONNECTION_TIMEOUT_SECONDS = 30;

        private HttpClient httpClient;
        private LoggingChannel logChannel;
        private ILoggingSession loggingSession;

        public Loader(ILoggingSession loggingSession)
        {
            this.loggingSession = loggingSession;

            httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(CONNECTION_TIMEOUT_SECONDS);
            logChannel = new LoggingChannel("LoaderLogChannel");
            loggingSession.AddLoggingChannel(logChannel);
        }

        public async Task<string> FetchEpisodeListAsync()
        {
            logChannel.LogMessage("Fetching episode list.", LoggingLevel.Verbose);
            return await httpClient.GetStringAsync(MAIN_URL);
        }

        public async Task<string> FetchEpisodePageAsync(Episode episode)
        {
            logChannel.LogMessage("Fetching episode page.", LoggingLevel.Verbose);
            return await httpClient.GetStringAsync(MAIN_URL + episode.Url);
        }

        public async Task DownloadEpisodeAsync(Episode episode, ICollection<string> urls)
        {
            string message;
            int partNumber = 0;
            foreach (string url in urls)
            {
                partNumber++;
                message = string.Format(
                    "Starting download part {0} of {1} into file",
                    partNumber,
                    urls.Count);
                logChannel.LogMessage(message, LoggingLevel.Verbose);

                using (HttpResponseMessage response = await httpClient.GetAsync(url,
                    HttpCompletionOption.ResponseHeadersRead))
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        message = string.Format(
                            "Server connection error. Status {0} {1}",
                            response.StatusCode,
                            response.ReasonPhrase);
                        logChannel.LogMessage(message, LoggingLevel.Error);
                    }

                    message = string.Format("Have to download {0} bytes", 
                        response.Content.Headers.ContentLength);
                    logChannel.LogMessage(message, LoggingLevel.Verbose);
                    using (Stream streamToReadFrom = await response.Content.ReadAsStreamAsync())
                    using (Stream streamToWriteTo = await FileManager.GetStreamForWrite(episode.Name, partNumber))
                    {
                        logChannel.LogMessage("Download started.", LoggingLevel.Verbose);
                        await streamToReadFrom.CopyToAsync(streamToWriteTo);
                        logChannel.LogMessage("Download complete.", LoggingLevel.Verbose);
                    }                    
                }
            }
        }

        public void Dispose()
        {
            loggingSession.RemoveLoggingChannel(logChannel);
            logChannel.Dispose();
            httpClient.Dispose();
        }
    }
}
