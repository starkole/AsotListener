namespace AsotListener.Services.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Contracts;
    using Models;
    using Models.Enums;
    using Windows.ApplicationModel.Core;
    using Windows.Foundation.Diagnostics;
    using Windows.Networking.BackgroundTransfer;
    using Windows.UI.Core;
    using Windows.UI.Popups;

    using static Models.Enums.EpisodeStatus;

    /// <summary>
    /// Contains logic for managing episode downloading
    /// </summary>
    public sealed class DownloadManager : IDownloadManager
    {
        #region Fields

        private const double defaultEpisodeSize = 400 * 1024 * 1024; // 400MB
        private Dictionary<Episode, List<DownloadOperation>> activeDownloadsByEpisode;
        private Dictionary<DownloadOperation, Episode> activeDownloadsByDownload;
        private readonly ILogger logger;
        private readonly ILoaderFactory loaderFactory;
        private readonly IParser parser;
        private readonly IApplicationSettingsHelper applicationSettingsHelper;
        private readonly IFileUtils fileUtils;

        #endregion

        #region Properties

        private EpisodeList episodeList => EpisodeList.Instance;

        /// <summary>
        /// The result of the asynchronous initialization of this instance.
        /// </summary>
        public Task Initialization { get; }

        #endregion

        #region Ctor

        /// <summary>
        /// Creates new instance of <see cref="DownloadManager"/>
        /// </summary>
        /// <param name="logger">Instance of <see cref="ILogger"/></param>
        /// <param name="playlist">Instance of <see cref="IPlayList"/></param>
        /// <param name="loaderFactory">Instance of <see cref="ILoaderFactory"/></param>
        /// <param name="parser">Instance of <see cref="IParser"/></param>
        /// <param name="fileUtils">Instance of <see cref="IFileUtils"/></param>
        public DownloadManager(
            ILogger logger,
            ILoaderFactory loaderFactory,
            IParser parser,
            IApplicationSettingsHelper applicationSettingsHelper,
            IFileUtils fileUtils)
        {
            this.logger = logger;
            this.loaderFactory = loaderFactory;
            this.parser = parser;
            this.applicationSettingsHelper = applicationSettingsHelper;
            this.fileUtils = fileUtils;

            Initialization = RetrieveActiveDownloads();
            logger.LogMessage("DownloadManager: Initialized.", LoggingLevel.Information);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Retrieves currently running downloads, 
        /// registers them in current <see cref="DownloadManager"/> instance
        /// and updates <see cref="EpisodeList"/> accordingly
        /// </summary>
        /// <returns>Task, which completes when all downloads has been processed</returns>
        public async Task RetrieveActiveDownloads()
        {
            await applicationSettingsHelper.Initialization;
            logger.LogMessage("EpisodesViewModel: Obtaining background downloads...");
            activeDownloadsByEpisode = new Dictionary<Episode, List<DownloadOperation>>();
            activeDownloadsByDownload = new Dictionary<DownloadOperation, Episode>();
            IReadOnlyList<DownloadOperation> downloads = await BackgroundDownloader.GetCurrentDownloadsAsync();
            logger.LogMessage($"EpisodesViewModel: {downloads.Count} background downloads found.", LoggingLevel.Information);
            foreach (var download in downloads)
            {
                string filename = download.ResultFile.Name;
                string episodeName = fileUtils.ExtractEpisodeNameFromFilename(filename);
                var episode = episodeList.FirstOrDefault(e => e.Name.StartsWith(episodeName, StringComparison.CurrentCulture));
                if (episode == null)
                {
                    logger.LogMessage($"EpisodesViewModel: Stale download detected. Stopping download from {download.RequestedUri} to {download.ResultFile.Name}", LoggingLevel.Warning);
                    download.AttachAsync().Cancel();
                    fileUtils.TryDeleteFile(filename).Start();
                    break;
                }

                episode.Status = Downloading;
                handleDownloadAsync(download, episode, DownloadState.AlreadyRunning);
            }
            logger.LogMessage("EpisodesViewModel: Background downloads processed.");
        }

        /// <summary>
        /// Schedules given episode to be downloaded
        /// </summary>
        /// <param name="episode">Episode to download</param>
        /// <returns>Task, which completes when episode download has been scheduled</returns>
        public async Task DownloadEpisode(Episode episode)
        {
            logger.LogMessage("EpisodesViewModel: Downloading episode...");
            if (episode == null)
            {
                logger.LogMessage("EpisodesViewModel: Invalid episode specified for download.", LoggingLevel.Warning);
                return;
            }

            using (ILoader loader = loaderFactory.GetLoader())
            {
                string episodePage = await loader.FetchEpisodePageAsync(episode);
                episode.DownloadLinks = parser.ExtractDownloadLinks(episodePage);
            }

            episode.Status = Downloading;
            for (var i = 0; i < episode.DownloadLinks.Length; i++)
            {
                await scheduleDownloadAsync(episode, i);
            }
            logger.LogMessage($"EpisodesViewModel: All downloads for episode {episode.Name} have been scheduled successfully.");
        }

        /// <summary>
        /// Cancels downloading of given episode
        /// </summary>
        /// <param name="episode">Episode to cancel download of</param>
        public void CancelDownload(Episode episode)
        {
            logger.LogMessage("EpisodesViewModel: Cancelling download...");
            if (episode == null)
            {
                logger.LogMessage("EpisodesViewModel: Cannot cancel downloads. Invalid episode specified. ", LoggingLevel.Warning);
                return;
            }

            if (episode.Status != Downloading)
            {
                logger.LogMessage("EpisodesViewModel: Cannot cancel downloads. Episode status is wrong.", LoggingLevel.Warning);
                return;
            }

            if (!activeDownloadsByEpisode.ContainsKey(episode))
            {
                logger.LogMessage("EpisodesViewModel: Cannot cancel downloads. No downloads list found.", LoggingLevel.Warning);
                return;
            }

            if (!activeDownloadsByEpisode[episode].Any())
            {
                logger.LogMessage("EpisodesViewModel: Cannot cancel downloads. Downloads list is empty.", LoggingLevel.Warning);
                return;
            }

            // Making shallow copy of the list here because on cancelling episode, 
            // handleDownloadAsync method will delete if from original list.
            var episodeDownloads = activeDownloadsByEpisode[episode].ToList();
            var filesToClear = episodeDownloads.Select(d => d.ResultFile.Name).ToList();
            foreach (var download in episodeDownloads)
            {
                logger.LogMessage($"EpisodesViewModel: Stopping download from {download.RequestedUri} to {download.ResultFile.Name}", LoggingLevel.Warning);
                download.AttachAsync().Cancel();
            }
            foreach (var file in filesToClear)
            {
                fileUtils.TryDeleteFile(file);
            }
            logger.LogMessage($"EpisodesViewModel: All downloads for episode {episode.Name} have been cancelled successfully.");
        }

        #endregion

        #region Private Methods

        private async Task scheduleDownloadAsync(Episode episode, int partNumber)
        {
            logger.LogMessage($"EpisodesViewModel: Scheduling download part {partNumber + 1} of {episode.DownloadLinks.Length}.");
            var downloader = new BackgroundDownloader();
            var uri = new Uri(episode.DownloadLinks[partNumber]);
            var file = await fileUtils.CreateEpisodePartFile(episode.Name, partNumber);
            if (file == null)
            {
                string message = "Cannot create file to save episode.";
                logger.LogMessage(message, LoggingLevel.Error);
                CoreDispatcher dispatcher = CoreWindow.GetForCurrentThread().Dispatcher;
                await dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                {
                    MessageDialog MDialog = new MessageDialog(message, "Error in ASOT Listener");
                    await MDialog.ShowAsync();
                });
                return;
            }

            var download = downloader.CreateDownload(uri, file);
            download.CostPolicy = BackgroundTransferCostPolicy.UnrestrictedOnly;
            handleDownloadAsync(download, episode, DownloadState.NotStarted);
            logger.LogMessage($"EpisodesViewModel: Successfully scheduled download from {episode.DownloadLinks[partNumber]} to {file.Name}.");
        }

        private async void handleDownloadAsync(DownloadOperation download, Episode episode, DownloadState downloadState)
        {
            try
            {
                logger.LogMessage($"EpisodesViewModel: Registering download of file {download.ResultFile.Name}.", LoggingLevel.Information);
                activeDownloadsByDownload.Add(download, episode);
                if (activeDownloadsByEpisode.Keys.Contains(episode))
                {
                    activeDownloadsByEpisode[episode].Add(download);
                }
                else
                {
                    activeDownloadsByEpisode.Add(episode, new List<DownloadOperation> { download });
                }

                Progress<DownloadOperation> progressCallback = new Progress<DownloadOperation>(downloadProgress);
#if DEBUG
                download.CostPolicy = BackgroundTransferCostPolicy.Always;
#endif
                if (downloadState == DownloadState.NotStarted)
                {
                    logger.LogMessage($"EpisodesViewModel: Download hasn't been started yet. Starting it.");
                    await download.StartAsync().AsTask(progressCallback);
                }
                if (downloadState == DownloadState.AlreadyRunning)
                {
                    logger.LogMessage($"EpisodesViewModel: Download has been already started. Attaching progress handler to it.");
                    await download.AttachAsync().AsTask(progressCallback);
                }

                ResponseInformation response = download.GetResponseInformation();
                logger.LogMessage($"EpisodesViewModel: Download of {download.ResultFile.Name} completed. Status Code: {response.StatusCode}", LoggingLevel.Information);
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => episode.Status = Loaded);
            }
            catch (TaskCanceledException)
            {
                logger.LogMessage("Download cancelled.");
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => episode.Status = CanBeLoaded);
                await fileUtils.TryDeleteFile(download.ResultFile.Name);
            }
            catch (Exception ex)
            {
                logger.LogMessage($"EpisodesViewModel: Download error. {ex.Message}", LoggingLevel.Error);
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => episode.Status = CanBeLoaded);
                await fileUtils.TryDeleteFile(download.ResultFile.Name);
            }
            finally
            {
                unregisterDownlod(download, episode);
            }
        }

        private void unregisterDownlod(DownloadOperation download, Episode episode)
        {
            logger.LogMessage($"EpisodesViewModel: Unregistering download of file {download.ResultFile.Name}.");
            if (activeDownloadsByDownload.ContainsKey(download))
            {
                activeDownloadsByDownload.Remove(download);
            }

            if (activeDownloadsByEpisode.ContainsKey(episode))
            {
                activeDownloadsByEpisode[episode].Remove(download);
                if (!activeDownloadsByEpisode[episode].Any())
                {
                    logger.LogMessage($"EpisodesViewModel: No downloads left for episode {episode.Name}. Unregistering episode.");
                    activeDownloadsByEpisode.Remove(episode);
                }
            }

            logger.LogMessage($"EpisodesViewModel: Download of file {download.ResultFile.Name} has been unregistered.");
        }

        private async void downloadProgress(DownloadOperation download)
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                if (!activeDownloadsByDownload.ContainsKey(download))
                {
                    logger.LogMessage($"EpisodesViewModel: Tried to report progress for download of {download.ResultFile.Name} file. But it is not registered.", LoggingLevel.Warning);
                    return;
                }

                Episode episode = activeDownloadsByDownload[download];
                if (!activeDownloadsByEpisode.ContainsKey(episode) || !activeDownloadsByEpisode[episode].Any())
                {
                    logger.LogMessage($"EpisodesViewModel: Tried to report progress for {episode.Name} episode download. But they are not registered.", LoggingLevel.Warning);
                    return;
                }

                episode.OverallDownloadSize = activeDownloadsByEpisode[episode].Sum((Func<DownloadOperation, double>)getTotalBytesToDownload);
                episode.DownloadedAmount = activeDownloadsByEpisode[episode].Sum(d => (double)d.Progress.BytesReceived);
                logger.LogMessage($"EpisodesViewModel: {episode.Name}: downloaded {episode.DownloadedAmount} of {episode.OverallDownloadSize} bytes.");
            });
        }

        private double getTotalBytesToDownload(DownloadOperation download)
        {
            var total = download.Progress.TotalBytesToReceive;

            // Some servers don't return file size to download, 
            // so we are using default approximated value in such a case.
            return total == 0 ? defaultEpisodeSize : total;
        }

        #endregion

    }
}
