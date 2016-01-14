namespace AsotListener.Services.Implementations
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Models;
    using Contracts;
    using Windows.Foundation.Diagnostics;

    using static Models.Enums.EpisodeStatus;

    /// <summary>
    /// Contains logic for managing episode list
    /// </summary>
    public sealed class EpisodeListManager : IEpisodeListManager
    {
        #region Fields

        private readonly ILogger logger;
        private readonly IFileUtils fileUtils;
        private readonly IApplicationSettingsHelper applicationSettingsHelper;
        private readonly ILoaderFactory loaderFactory;

        #endregion

        #region Properties

        private EpisodeList episodeList => EpisodeList.Instance;
        private Playlist playlist => Playlist.Instance;

        /// <summary>
        /// Information about current instance initialization
        /// </summary>
        public Task Initialization { get; }

        #endregion

        #region Ctor

        /// <summary>
        /// Creates new instance of <see cref="EpisodeListManager"/>
        /// </summary>
        /// <param name="logger">Instance of <see cref="ILogger"/></param>
        /// <param name="fileUtils">Instance of <see cref="IFileUtils"/></param>
        /// <param name="applicationSettingsHelper">Instance of <see cref="IApplicationSettingsHelper"/></param>
        /// <param name="loaderFactory">Instance of <see cref="ILoaderFactory"/></param>
        public EpisodeListManager(
           ILogger logger,
           IFileUtils fileUtils,
           IApplicationSettingsHelper applicationSettingsHelper,
           ILoaderFactory loaderFactory)
        {
            this.loaderFactory = loaderFactory;
            this.applicationSettingsHelper = applicationSettingsHelper;
            this.fileUtils = fileUtils;
            this.logger = logger;
            Initialization = initializeAsync();
        }

        private async Task initializeAsync()
        {
            await applicationSettingsHelper.Initialization;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Adds episode to playlist
        /// </summary>
        /// <param name="episode">Episode to add</param>
        /// <returns>Awaitable <see cref="Task"/></returns>
        public async Task AddEpisodeToPLaylistAsync(Episode episode)
        {
            logger.LogMessage("EpisodeListManager: Executing add to playlist command...");
            if (episode == null)
            {
                logger.LogMessage($"EpisodeListManager: Cannot add empty episode to playlist.", LoggingLevel.Warning);
                return;
            }

            if (episode.Status != Loaded)
            {
                logger.LogMessage($"EpisodeListManager: Cannot add episode to playlist. It has invalid status.", LoggingLevel.Warning);
                return;
            }

            playlist.AddEpisodeFiles(episode.Name, await fileUtils.GetFilesWithDurationsForEpisode(episode.Name));
            await applicationSettingsHelper.SavePlaylistWithNotification();
            episode.Status = InPlaylist;
            logger.LogMessage("EpisodeListManager: Add to playlist command executed.");
        }

        /// <summary>
        /// Deletes all episode data from local storage
        /// </summary>
        /// <param name="episode">Episode whose data will be deleted</param>
        /// <returns>Awaitable <see cref="Task"/></returns>
        public async Task DeleteEpisodeDataAsync(Episode episode)
        {
            logger.LogMessage($"EpisodeListManager: Deleting episode...");
            if (canEpisodeBeDeleted(episode))
            {
                var tracksToRemove = playlist.Where(t => t.EpisodeName == episode.Name).ToList();
                bool isPlaylistAffected = tracksToRemove.Any();
                foreach (var track in tracksToRemove)
                {
                    playlist.Remove(track);
                }
                if (isPlaylistAffected)
                {
                    await applicationSettingsHelper.SavePlaylistWithNotification();
                }
                await fileUtils.DeleteEpisode(episode.Name);
            }
            episode.Status = CanBeLoaded;
            logger.LogMessage($"EpisodeListManager: Episode has been deleted.");
        }

        /// <summary>
        /// Adds episode to playlist, if it hadn't been added, and starts playing it
        /// </summary>
        /// <param name="episode">Episode to play</param>
        /// <returns>Awaitable <see cref="Task"/></returns>
        public async Task PlayEpisodeAsync(Episode episode)
        {
            logger.LogMessage("EpisodeListManager: Scheduling episode playback episode...");
            if (episode == null)
            {
                logger.LogMessage($"EpisodeListManager: Cannot play empty episode.", LoggingLevel.Warning);
                return;
            }

            if (!(episode.Status == Loaded || episode.Status == InPlaylist))
            {
                logger.LogMessage($"EpisodeListManager: Cannot play episode. It has invalid status.", LoggingLevel.Warning);
                return;
            }

            playlist.CurrentTrack = playlist.AddEpisodeFiles(episode.Name, await fileUtils.GetFilesWithDurationsForEpisode(episode.Name));
            await applicationSettingsHelper.SavePlaylistWithNotification();

            episode.Status = InPlaylist;
            logger.LogMessage("EpisodeListManager: Episode scheduled to play.");
        }

        /// <summary>
        /// Updates statuses of episodes in episode list
        /// </summary>
        /// <returns>Awaitable <see cref="Task"/></returns>
        public async Task UpdateEpisodeStatesAsync()
        {
            logger.LogMessage("EpisodeListManager: Updating episode states...");
            if (episodeList == null || !episodeList.Any())
            {
                logger.LogMessage("EpisodeListManager: Cannot update episode states. Episode list is empty.", LoggingLevel.Warning);
                return;
            }

            var existingFileNames = await fileUtils.GetDownloadedFileNamesList();
            foreach (Episode episode in episodeList)
            {
                if (episode.Status == Downloading)
                {
                    return;
                }

                if (existingFileNames.Contains(episode.Name))
                {
                    if (playlist.Contains(episode))
                    {
                        episode.Status = InPlaylist;
                        continue;
                    }

                    episode.Status = Loaded;
                    continue;
                }
            }
            logger.LogMessage("EpisodeListManager: Episode states has been updated successfully.");
        }

        /// <summary>
        /// Loads fresh copy of episode list from server
        /// </summary>
        /// <returns>Number of new episodes</returns>
        public async Task<int> LoadEpisodeListFromServerAsync()
        {
            logger.LogMessage("EpisodeListManager: Loading episode list from server...");
            var oldEpisodesCount = episodeList.Count;
            using (ILoader loader = loaderFactory.GetLoader())
            {
                await loader.FetchEpisodeListAsync();
            }
            await applicationSettingsHelper.SaveEpisodeList();
            await UpdateEpisodeStatesAsync();
            logger.LogMessage("EpisodeListManager: Episode list loaded.", LoggingLevel.Information);
            return episodeList.Count - oldEpisodesCount;
        }

        /// <summary>
        /// Returns episode by given number, or null if there is no such episode
        /// </summary>
        /// <param name="number">Episode number</param>
        /// <returns><see cref="Episode"/> with given number or null</returns>
        public Episode GetEpisodeByNumber(int number)
        {
            var match = " " + number.ToString() + " ";
            var result = episodeList.FirstOrDefault(e => e.Name.Contains(match));
            return result;
        }

        /// <summary>
        /// Plays the last downloaded episode
        /// </summary>
        /// <returns>Awaitable <see cref="Task"/></returns>
        public async Task PlayLastDownloadedEpisodeAsync()
        {
            logger.LogMessage("EpisodeListManager: Playing the last downloaded episode...");
            var ep = episodeList.FirstOrDefault(e => e.CanBePlayed);
            if (ep == null)
            {
                logger.LogMessage("EpisodeListManager: Cannot play the last downloaded episode. No such found.", LoggingLevel.Warning);
                return;
            }

            playlist.Clear();
            await PlayEpisodeAsync(ep);
            await UpdateEpisodeStatesAsync();
        }

        #endregion

        #region Private Methods

        private static bool canEpisodeBeDeleted(Episode episode) =>
            episode != null &&
            (episode.Status == Loaded ||
            episode.Status == InPlaylist);

        #endregion
    }
}
