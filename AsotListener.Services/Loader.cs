namespace AsotListener.Services
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Models;
    using Windows.Foundation.Diagnostics;

    public class Loader : ILoader
    {
        private const string MAIN_URL = "http://asotarchive.org";
        private const string FALLBACK_FILENAME = "file";
        private const string FILE_EXTENSION = ".mp3";
        private const int CONNECTION_TIMEOUT_SECONDS = 30;

        private HttpClient httpClient;
        private LoggingChannel logChannel;

        public Loader(LoggingChannel logChannel)
        {
            httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(CONNECTION_TIMEOUT_SECONDS);
            logChannel = new LoggingChannel("LoaderLogChannel");
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

        public async Task DownloadEpisodeAsync(ICollection<string> urls, string filename)
        {
            string message;

            if (urls == null || urls.Count < 1)
            {
                return;
            }

            if (string.IsNullOrEmpty(filename))
            {
                filename = FALLBACK_FILENAME;
            }

            for (int i = 0; i < urls.Count; i++)
            {
                int fileId = i + 1;
                string episodeFilename = Path.GetFullPath(filename + fileId.ToString() + FILE_EXTENSION);
                message = string.Format(
                    "Starting download part {0} of {1} into {2} file",
                    fileId,
                    urls.Count,
                    episodeFilename);
                logChannel.LogMessage(message, LoggingLevel.Verbose);

                using (HttpResponseMessage response = await httpClient.GetAsync(
                    urls[i],
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
                    {
                        string fileToWriteTo = Path.GetTempFileName();
                        using (Stream streamToWriteTo = File.Open(
                            fileToWriteTo, 
                            FileMode.Create))
                        {
                            logChannel.LogMessage("Download started.", LoggingLevel.Verbose);
                            await streamToReadFrom.CopyToAsync(streamToWriteTo);
                            logChannel.LogMessage("Download complete.", LoggingLevel.Verbose);
                        }
                    }
                }
            }
        }

        public void Dispose()
        {
            logChannel.Dispose();
            httpClient.Dispose();
        }
    }
}
