namespace AsotListener.Services.Implementations
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Models;
    using Contracts;
    using Windows.Foundation.Diagnostics;

    using static Models.Enums.EpisodeStatus;

    public sealed class EpisodeListManager : IEpisodeListManager
    {
        private readonly ILogger logger;
        private readonly IFileUtils fileUtils;
        readonly IApplicationSettingsHelper applicationSettingsHelper;

        private EpisodeList episodeList => EpisodeList.Instance;
        private Playlist playlist => Playlist.Instance;

        public Task Initialization { get; }

        public EpisodeListManager(ILogger logger, IFileUtils fileUtils, IApplicationSettingsHelper applicationSettingsHelper)
        {
            this.applicationSettingsHelper = applicationSettingsHelper;
            this.fileUtils = fileUtils;
            this.logger = logger;
            Initialization = initializeAsync();
        }

        private async Task initializeAsync()
        {
            await applicationSettingsHelper.Initialization;
        }

        public async Task AddEpisodeToPLaylist(Episode episode)
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

            playlist.AddEpisodeFiles(episode.Name, await fileUtils.GetFilesListForEpisode(episode.Name));
            await applicationSettingsHelper.SavePlaylistWithNotification();
            episode.Status = InPlaylist;
            logger.LogMessage("EpisodeListManager: Add to playlist command executed.");
        }

        public async Task DeleteEpisodeData(Episode episode)
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

        public async Task PlayEpisode(Episode episode)
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

            playlist.CurrentTrack = playlist.AddEpisodeFiles(episode.Name, await fileUtils.GetFilesListForEpisode(episode.Name));
            await applicationSettingsHelper.SavePlaylistWithNotification();

            episode.Status = InPlaylist;
            logger.LogMessage("EpisodeListManager: Episode scheduled to play.");
        }

        public async Task UpdateEpisodeStates()
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

        private static bool canEpisodeBeDeleted(Episode episode) =>
            episode != null &&
            (episode.Status == Loaded ||
            episode.Status == InPlaylist);
    }
}
