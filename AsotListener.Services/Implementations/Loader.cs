namespace AsotListener.Services.Implementations
{
    using System;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Contracts;
    using Models;
    using Windows.Foundation.Diagnostics;

    /// <summary>
    /// Contains logic for loading data over HTTP protocol
    /// </summary>
    public sealed class Loader : ILoader
    {
        private const string mainUrl = "http://asotarchive.org";
        private const int connectionTimeoutSeconds = 30;

        private readonly HttpClient httpClient;
        private readonly ILogger logger;
        private readonly IParser parser;

        private EpisodeList EpisodeList => EpisodeList.Instance;

        /// <summary>
        /// Creates instance of <see cref="Loader"/>
        /// </summary>
        /// <param name="logger">The logger instance</param>
        /// <param name="parser">The <see cref="IParser"/> instance</param>
        public Loader(ILogger logger, IParser parser)
        {
            this.parser = parser;
            this.logger = logger;

            httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(connectionTimeoutSeconds);
            logger.LogMessage("Loader initialized.", LoggingLevel.Information);
        }

        /// <summary>
        /// Loads episode list page
        /// </summary>
        /// <returns>Task, completed when episode list has been loaded</returns>
        public async Task FetchEpisodeListAsync()
        {
            try
            {
                logger.LogMessage("Loader: Fetching episode list.");
                var episodeListPage = await httpClient.GetStringAsync(mainUrl);
                var episodes = parser.ParseEpisodeList(episodeListPage);
                EpisodeList.Clear();
                EpisodeList.AddRange(episodes);
            }
            catch (Exception ex)
            {
                logger.LogMessage($"Loader: Error loading episode list. {ex.Message}", LoggingLevel.Error);
            }
        }

        /// <summary>
        /// Loads description page for specific episode
        /// </summary>
        /// <param name="episode">Episode url</param>
        /// <returns>Description page for specific episode</returns>
        public async Task<string> FetchEpisodePageAsync(Episode episode)
        {
            try
            {
                logger.LogMessage("Loader: Fetching episode page.");
                return await httpClient.GetStringAsync(mainUrl + episode.Url);
            }
            catch (Exception ex)
            {
                logger.LogMessage($"Loader: Error loading episode page. {ex.Message}", LoggingLevel.Error);
                return string.Empty;
            }
        }

        /// <summary>
        /// Closes connections and releases used resources
        /// </summary>
        public void Dispose()
        {
            httpClient.Dispose();
        }
    }
}
