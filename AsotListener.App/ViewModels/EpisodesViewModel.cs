namespace AsotListener.App.ViewModels
{
    using System;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Threading.Tasks;
    using Services.Contracts;
    using Models;
    using System.Windows.Input;
    using Windows.Foundation.Diagnostics;
    using Windows.Networking.BackgroundTransfer;
    using System.Collections.Generic;
    using Windows.UI.Core;
    using Windows.ApplicationModel.Core;
    using Models.Enums;
    using Windows.UI.Popups;
    using Windows.UI.Xaml;
    using Windows.ApplicationModel;
    using static Models.Enums.EpisodeStatus;

    public sealed class EpisodesViewModel : BaseModel
    {
        #region Fields

        private const string episodeListFileName = "episodeList.xml";
        private ObservableCollection<Episode> episodes;
        private Dictionary<Episode, List<DownloadOperation>> activeDownloadsByEpisode;
        private Dictionary<DownloadOperation, Episode> activeDownloadsByDownload;
        private readonly ILogger logger;
        private readonly IFileUtils fileUtils;
        private readonly IPlayList playlist;
        private readonly ILoaderFactory loaderFactory;
        private readonly IParser parser;
        private readonly INavigationService navigationService;

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
            IParser parser,
            INavigationService navigationService)
        {
            this.logger = logger;
            this.fileUtils = fileUtils;
            this.playlist = playlist;
            this.loaderFactory = loaderFactory;
            this.parser = parser;
            this.navigationService = navigationService;

            RefreshCommand = new RelayCommand(loadEpisodeListFromServer);
            DownloadCommand = new RelayCommand(downloadEpisode);
            CancelDownloadCommand = new RelayCommand((Action<object>)cancelDownload);
            DeleteCommand = new RelayCommand((Action<object>)deleteEpisodeFromStorage);
            PlayCommand = new RelayCommand((Action<object>)playEpisode);
            AddToPlaylistCommand = new RelayCommand((Action<object>)addToPlaylistCommand);

            activeDownloadsByEpisode = new Dictionary<Episode, List<DownloadOperation>>();
            activeDownloadsByDownload = new Dictionary<DownloadOperation, Episode>();

            Application.Current.Suspending += onAppResuming;
            Application.Current.Suspending += onAppSuspending;
            
            initializeAsync();
        }

        private async void initializeAsync()
        {
            await loadEpisodesList();
            await retrieveActiveDownloads();
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
            await fileUtils.SaveToXmlFile(EpisodeList, episodeListFileName);
        }

        private async Task downloadEpisode(object boxedEpisode)
        {
            var episode = boxedEpisode as Episode;
            if (episode == null)
            {
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
                var downloader = new BackgroundDownloader();
                var uri = new Uri(episode.DownloadLinks[i]);
                var file = await fileUtils.GetEpisodePartFile(episode.Name, i);
                if (file == null)
                {
                    CoreDispatcher dispatcher = CoreWindow.GetForCurrentThread().Dispatcher;
                    await dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                    {
                        MessageDialog MDialog = new MessageDialog("Cannot create file to save episode.", "Error in ASOT Listener");
                        await MDialog.ShowAsync();
                    });
                    break;
                }

                var download = downloader.CreateDownload(uri, file);
                download.CostPolicy = BackgroundTransferCostPolicy.UnrestrictedOnly;
                handleDownloadAsync(download, episode, DownloadState.NotStarted);
            }
        }

        private void cancelDownload(object boxedEpisode)
        {
            var episode = boxedEpisode as Episode;
            if (episode == null && episode.Status != Downloading)
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
            episode.Status = CanBeLoaded;
        }

        private async void playEpisode(object boxedEpisode)
        {
            var episode = boxedEpisode as Episode;
            if (episode == null && episode.Status != Loaded)
            {
                return;
            }

            var existingTracks = playlist.TrackList.Where(t => t.Name.StartsWith(episode.Name));
            playlist.CurrentTrack = existingTracks.Any() ?
                existingTracks.First() :
                await addEpisodeToPlaylist(episode);
            await playlist.SavePlaylistToLocalStorage();
            episode.Status = Playing;
            navigationService.Navigate(NavigationParameter.StartPlayback);
        }

        private async void addToPlaylistCommand(object boxedEpisode)
        {
            var episode = boxedEpisode as Episode;
            if (episode == null && episode.Status != Loaded)
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
            episode.Status = Playing;
        }

        #endregion

        #region Private Methods

        private async void onAppResuming(object sender, SuspendingEventArgs e)
        {
            await updateEpisodesStates();
            await retrieveActiveDownloads();
        }

        private async void onAppSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            await loadEpisodesList();
            deferral.Complete();
        }

        private async Task loadEpisodesList()
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
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => episode.Status = Loaded);
            }
            catch (TaskCanceledException)
            {
                logger.LogMessage("Download cancelled.");
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => episode.Status = CanBeLoaded);
            }
            catch (Exception ex)
            {
                logger.LogMessage($"Error {ex.Message}", LoggingLevel.Error);
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => episode.Status = CanBeLoaded);
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
                logger.LogMessage($"Downloaded {episode.DownloadedAmount} of {episode.OverallDownloadSize} bytes.");
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
                    episode.Status = Loaded;
                }

                if (activeDownloadsByEpisode.ContainsKey(episode))
                {
                    episode.Status = Downloading;
                }
            }
        }

        private bool canEpisodeBeDeleted(Episode episode) =>
            episode != null &&
            (episode.Status == Loaded ||
            episode.Status == Playing);

        #endregion
    }
}
