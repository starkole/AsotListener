namespace AsotListener.App.ViewModels
{
    using System;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Threading.Tasks;
    using Services.Contracts;
    using Models;
    using System.Windows.Input;
    using System.Runtime.Serialization;
    using System.IO;
    using Windows.Storage;
    using Windows.Foundation.Diagnostics;
    using Windows.Storage.Streams;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml;
    using Windows.Networking.BackgroundTransfer;
    using System.Collections.Generic;
    using System.Threading;
    using Windows.UI.Core;
    using Windows.ApplicationModel.Core;

    public class EpisodesViewModel : BaseModel, IDisposable
    {
        #region Fields

        private const string episodeListFileName = "episodeList.xml";
        private ObservableCollection<Episode> episodes;
        private BackgroundDownloader backgroundDownloader;
        Dictionary<Episode, List<DownloadOperation>> activeDownloadsByEpisode;
        Dictionary<DownloadOperation, Episode> activeDownloadsByDownload;
        private readonly ILogger logger;
        private readonly IFileUtils fileUtils;
        private readonly IPlayList playlist;
        private readonly ILoaderFactory loaderFactory;
        private readonly IParser parser;

        #endregion

        #region Properties

        public ObservableCollection<Episode> EpisodeList
        {
            get { return episodes; }
            private set { SetField(ref episodes, value, nameof(EpisodeList)); }
        }

        public ICommand RefreshCommand { get; }
        public ICommand DownloadCommand { get; }
        public ICommand CancelDownloadCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand PlayCommand { get; }
        public ICommand AddToPlaylistCommand { get; }

        #endregion

        #region Ctor

        public EpisodesViewModel(
            ILogger logger,
            IFileUtils fileUtils,
            IPlayList playlist,
            ILoaderFactory loaderFactory,
            IParser parser)
        {
            this.logger = logger;
            this.fileUtils = fileUtils;
            this.playlist = playlist;
            this.loaderFactory = loaderFactory;
            this.parser = parser;

            RefreshCommand = new RelayCommand(loadEpisodeListFromServer);
            DownloadCommand = new RelayCommand(downloadEpisode);
            CancelDownloadCommand = new RelayCommand((Action<object>)cancelDownload);
            DeleteCommand = new RelayCommand((Action<object>)deleteEpisodeFromStorage);
            PlayCommand = new RelayCommand((Action<object>)playEpisode);
            AddToPlaylistCommand = new RelayCommand((Action<object>)addToPlaylistCommand);

            activeDownloadsByEpisode = new Dictionary<Episode, List<DownloadOperation>>();
            activeDownloadsByDownload = new Dictionary<DownloadOperation, Episode>();

            initializeModelAsync();
        }

        #endregion

        #region Public Methods

        public async Task RestoreEpisodesList()
        {
            EpisodeList = await fileUtils.ReadFromXmlFile<ObservableCollection<Episode>>(episodeListFileName);
            if (EpisodeList == null || !EpisodeList.Any())
            {
                await loadEpisodeListFromServer();
            }
            else
            {
                await updateEpisodesStates();
            }
        }

        public async Task StoreEpisodesList()
        {
            await fileUtils.SaveToXmlFile(EpisodeList, episodeListFileName);
        }

        #endregion

        #region Commands

        private async Task loadEpisodeListFromServer()
        {
            using (ILoader loader = loaderFactory.GetLoader())
            {
                string episodeListPage = await loader.FetchEpisodeListAsync();
                EpisodeList = parser.ParseEpisodeList(episodeListPage);
            }
            await updateEpisodesStates();
            await StoreEpisodesList();
        }

        private async Task downloadEpisode(object boxedEpisode)
        {
            var episode = boxedEpisode as Episode;
            if (episode == null)
            {
                return;
            }

            if (backgroundDownloader == null)
            {
                backgroundDownloader = new BackgroundDownloader();
            }

            using (ILoader loader = loaderFactory.GetLoader())
            {
                string episodePage = await loader.FetchEpisodePageAsync(episode);
                episode.DownloadLinks = parser.ExtractDownloadLinks(episodePage);
            }

            episode.Status = EpisodeStatus.Downloading;
            for (var i = 0; i < episode.DownloadLinks.Length; i++)
            {
                var downloader = new BackgroundDownloader();
                var uri = new Uri(episode.DownloadLinks[i]);
                var file = await fileUtils.GetEpisodePartFile(episode.Name, i);
                var download = downloader.CreateDownload(uri, file);
                download.CostPolicy = BackgroundTransferCostPolicy.UnrestrictedOnly;
                handleDownloadAsync(download, episode, DownloadState.NotStarted);
            }
        }

        private void cancelDownload(object boxedEpisode)
        {
            var episode = boxedEpisode as Episode;
            if (episode == null && episode.Status != EpisodeStatus.Downloading)
            {
                return;
            }

            // Making shallow copy of the list here because it will be affected by handleDownloadAsync method.
            var episodeDownloads = activeDownloadsByEpisode[episode].ToList(); //TODO: Integrity check
            foreach (var download in episodeDownloads)
            {
                logger.LogMessage($"Stopping download from {download.RequestedUri} to {download.ResultFile.Name}", LoggingLevel.Warning);
                download.AttachAsync().Cancel();
            }
        }

        private async void deleteEpisodeFromStorage(object boxedEpisode)
        {
            var episode = boxedEpisode as Episode;
            if (canEpisodeBeDeleted(episode))
            {
                await fileUtils.DeleteEpisode(episode.Name);
            }
            episode.Status = EpisodeStatus.CanBeLoaded;
        }

        private async void playEpisode(object boxedEpisode)
        {
            var episode = boxedEpisode as Episode;
            if (episode == null && episode.Status != EpisodeStatus.Loaded)
            {
                return;
            }

            var existingTracks = playlist.TrackList.Where(t => t.Name.StartsWith(episode.Name));
            playlist.CurrentTrack = existingTracks.Any() ?
                existingTracks.First() :
                await addEpisodeToPlaylist(episode);
            await playlist.SavePlaylistToLocalStorage();

            // Navigate to player and start playback
            // TODO: This is ugly and should be replaced with some NavigationService
            Frame rootFrame = Window.Current.Content as Frame;
            rootFrame.Navigate(typeof(MainPage), Constants.StartPlayback);
        }

        private async void addToPlaylistCommand(object boxedEpisode)
        {
            var episode = boxedEpisode as Episode;
            if (episode == null && episode.Status != EpisodeStatus.Loaded)
            {
                return;
            }

            var existingTracks = playlist.TrackList.Where(t => t.Name.StartsWith(episode.Name));
            if (existingTracks.Any())
            {
                return;
            }

            await addEpisodeToPlaylist(episode);
            await playlist.SavePlaylistToLocalStorage();
        }

        #endregion

        #region Private Methods
        private async void initializeModelAsync()
        {
            await RestoreEpisodesList();
            await retrieveActiveDownloads();
        }

        private async Task retrieveActiveDownloads()
        {
            logger.LogMessage("Obtaining background downloads...");
            IReadOnlyList<DownloadOperation> downloads = await BackgroundDownloader.GetCurrentDownloadsAsync();
            logger.LogMessage($"{downloads.Count} background downloads found.");
            foreach (var download in downloads)
            {
                string filename = download.ResultFile.Name;
                string episodeName = fileUtils.ExtractEpisodeNameFromFilename(filename);

                var episode = EpisodeList.Where(e => e.Name.StartsWith(episodeName)).FirstOrDefault();
                if (episode == null)
                {
                    logger.LogMessage($"Stale download detected. Stopping download from {download.RequestedUri} to {download.ResultFile.Name}", LoggingLevel.Warning);
                    download.AttachAsync().Cancel();
                    fileUtils.TryDeleteFile(filename).Start();
                    break;
                }
                
                handleDownloadAsync(download, episode, DownloadState.AlreadyRunning);
            }
        }

        private async void handleDownloadAsync(DownloadOperation download, Episode episode, DownloadState downloadState)
        {
            try
            {
                // Register download
                activeDownloadsByDownload.Add(download, episode);
                if (activeDownloadsByEpisode.Keys.Contains(episode))
                {
                    activeDownloadsByEpisode[episode].Add(download);
                }
                else
                {
                    activeDownloadsByEpisode.Add(episode, new List<DownloadOperation>() { download });
                }

                // Start and attach handlers
                Progress<DownloadOperation> progressCallback = new Progress<DownloadOperation>(downloadProgress);
                if (downloadState == DownloadState.NotStarted)
                {
                    await download.StartAsync().AsTask(progressCallback);
                }
                if (downloadState == DownloadState.AlreadyRunning)
                {
                    await download.AttachAsync().AsTask(progressCallback);
                }

                ResponseInformation response = download.GetResponseInformation();
                logger.LogMessage($"Download of {download.ResultFile.Name} completed. Status Code: {response.StatusCode}");
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => episode.Status = EpisodeStatus.Loaded);
            }
            catch (TaskCanceledException)
            {
                logger.LogMessage("Download cancelled.");
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => episode.Status = EpisodeStatus.CanBeLoaded);
            }
            catch (Exception ex)
            {
                logger.LogMessage($"Error {ex.Message}", LoggingLevel.Error);
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => episode.Status = EpisodeStatus.CanBeLoaded);
            }
            finally
            {
                // TODO: Add integrity checks and logging here, maybe extract to separate method  
                logger.LogMessage($"Unregistering download of file {download.ResultFile.Name}.");
                activeDownloadsByDownload.Remove(download);
                activeDownloadsByEpisode[episode].Remove(download);
                if (!activeDownloadsByEpisode[episode].Any())
                {
                    logger.LogMessage($"No downloads left for episode {episode.Name}. Unregistering episode.");
                    activeDownloadsByEpisode.Remove(episode);
                }
                logger.LogMessage($"Download of file {download.ResultFile.Name} has been unregistered.");
            }
        }

        private async void downloadProgress(DownloadOperation download)
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                Episode episode = activeDownloadsByDownload[download];
                episode.OverallDownloadSize = activeDownloadsByEpisode[episode].Sum((Func<DownloadOperation, double>)getTotalBytesToDownload);
                episode.DownloadedAmount = activeDownloadsByEpisode[episode].Sum(d => (double)d.Progress.BytesReceived);
            });
        }

        private double getTotalBytesToDownload(DownloadOperation download)
        {
            var total = download.Progress.TotalBytesToReceive;
            return total == 0 ? Constants.DefaultEpisodeSize : total;
        }

        private async Task<AudioTrack> addEpisodeToPlaylist(Episode episode)
        {
            var fileNames = await fileUtils.GetFilesListForEpisode(episode.Name);
            int counter = 0;
            AudioTrack firstAddedTrack = null;
            foreach (var file in fileNames)
            {
                counter++;
                var track = new AudioTrack()
                {
                    Name = episode.Name + " Part " + counter.ToString(),
                    Uri = file.Path
                };

                if (counter == 1)
                {
                    firstAddedTrack = track;
                }
            }

            return firstAddedTrack;
        }

        private async Task updateEpisodesStates()
        {
            if (EpisodeList == null || !EpisodeList.Any())
            {
                return;
            }

            var existingFileNames = await fileUtils.GetDownloadedFileNamesList();
            foreach (Episode episode in EpisodeList)
            {
                if (existingFileNames.Contains(episode.Name))
                {
                    episode.Status = EpisodeStatus.Loaded;
                }

                if (activeDownloadsByEpisode.ContainsKey(episode))
                {
                    episode.Status = EpisodeStatus.Downloading;
                }
            }
        }

        private bool canEpisodeBeDeleted(Episode episode) =>
            episode != null &&
            (episode.Status == EpisodeStatus.Loaded ||
            episode.Status == EpisodeStatus.Playing);

        #endregion

        #region IDisposable Support

        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }

                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }

        #endregion
    }
}
