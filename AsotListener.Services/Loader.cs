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
        private ILogger logger;

        public Loader(ILogger logger)
        {
            this.logger = logger;

            httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(CONNECTION_TIMEOUT_SECONDS);
        }

        public async Task<string> FetchEpisodeListAsync()
        {
            logger.LogMessage("Loader: fetching episode list.");
            return await httpClient.GetStringAsync(MAIN_URL);
        }

        public async Task<string> FetchEpisodePageAsync(Episode episode)
        {
            logger.LogMessage("Loader: fetching episode page.");
            return await httpClient.GetStringAsync(MAIN_URL + episode.Url);
        }

        public async Task DownloadEpisodeAsync(Episode episode, ICollection<string> urls)
        {
            int partNumber = 0;
            foreach (string url in urls)
            {
                partNumber++;
                logger.LogMessage($"Loader: starting download part {partNumber} of {urls.Count} into file");

                using (HttpResponseMessage response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        logger.LogMessage($"Loader: server connection error. Status {response.StatusCode} {response.ReasonPhrase}", LoggingLevel.Error);
                    }

                    logger.LogMessage($"Loader: Have to download {response.Content.Headers.ContentLength} bytes");
                    using (Stream streamToReadFrom = await response.Content.ReadAsStreamAsync())
                    using (Stream streamToWriteTo = await FileManager.GetStreamForWrite(episode.Name, partNumber))
                    {
                        logger.LogMessage("Loader: Download started.");
                        await streamToReadFrom.CopyToAsync(streamToWriteTo);
                        logger.LogMessage("Loader: Download complete.");
                    }                    
                }
            }
        }

        public void Dispose()
        {
            httpClient.Dispose();
        }
    }
}
