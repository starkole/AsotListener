namespace AsotListener.Services.Implementations
{
    using System;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Contracts;
    using Models;
    using Windows.Foundation.Diagnostics;

    public sealed class Loader : ILoader
    {
        private const string mainUrl = "http://asotarchive.org";
        private const string fallbackFilename = "file";
        private const string fileExtension = ".mp3";
        private const int connectionTimeoutSeconds = 30;

        private readonly HttpClient httpClient;
        private ILogger logger;
        private IFileUtils fileUtils;

        public Loader(ILogger logger, IFileUtils fileUtils)
        {
            this.logger = logger;
            this.fileUtils = fileUtils;

            httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(connectionTimeoutSeconds);
            logger.LogMessage("Loader initialized.", LoggingLevel.Information);
        }

        public async Task<string> FetchEpisodeListAsync()
        {
            logger.LogMessage("Loader: Fetching episode list.");
            return await httpClient.GetStringAsync(mainUrl);
        }

        public async Task<string> FetchEpisodePageAsync(Episode episode)
        {
            logger.LogMessage("Loader: Fetching episode page.");
            return await httpClient.GetStringAsync(mainUrl + episode.Url);
        }

        public void Dispose()
        {
            httpClient.Dispose();
        }
    }
}
