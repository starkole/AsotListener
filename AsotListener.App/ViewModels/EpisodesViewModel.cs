namespace AsotListener.App.ViewModels
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows.Input;
    using Common;
    using Models;
    using Models.Enums;
    using Services.Contracts;
    using Windows.ApplicationModel.Background;
    using Windows.ApplicationModel.Core;
    using Windows.Foundation.Diagnostics;
    using Windows.UI.Core;
    using Windows.UI.Xaml;

    /// <summary>
    /// View model of episodes list
    /// </summary>
    public sealed class EpisodesViewModel : BaseModel, IAsyncInitialization
    {
        #region Fields

        private readonly IBackgroundTaskRegistration backgroundUpdaterTask;
        private readonly ILogger logger;
        private readonly INavigationService navigationService;
        private readonly IApplicationSettingsHelper applicationSettingsHelper;
        private readonly IDownloadManager downloadManager;
        private readonly IEpisodeListManager episodeListManager;

        #endregion

        #region Properties

        /// <summary>
        /// A list of available episodes
        /// </summary>
        public EpisodeList EpisodeList => EpisodeList.Instance;

        /// <summary>
        /// Downloads fresh episodes list from server
        /// </summary>
        public ICommand RefreshCommand { get; } // TODO: This should be moved to the MainPage VM

        /// <summary>
        /// Downloads specified episode
        /// </summary>
        public ICommand DownloadCommand { get; }

        /// <summary>
        /// Cancels episode download
        /// </summary>
        public ICommand CancelDownloadCommand { get; }

        /// <summary>
        /// Deletes all downloaded data of given episode
        /// </summary>
        public ICommand DeleteCommand { get; }

        /// <summary>
        /// Starts playing given episode in player
        /// </summary>
        public ICommand PlayCommand { get; }

        /// <summary>
        /// Adds given episode to playlist
        /// </summary>
        public ICommand AddToPlaylistCommand { get; }

        /// <summary>
        /// Clears current playlist
        /// </summary>
        public ICommand ClearPlaylistCommand { get; }

        /// <summary>
        /// The result of the asynchronous initialization.
        /// </summary>
        public Task Initialization { get; }

        #endregion

        #region Ctor

        /// <summary>
        /// Creates new instance of <see cref="EpisodesViewModel"/>
        /// </summary>
        /// <param name="logger">Instance of <see cref="ILogger"/></param>
        /// <param name="playlist">Instance of <see cref="IPlayList"/></param>
        /// <param name="loaderFactory">Instance of <see cref="ILoaderFactory"/></param>
        /// <param name="navigationService">Instance of <see cref="INavigationService"/></param>
        /// <param name="applicationSettingsHelper">Instance of <see cref="IApplicationSettingsHelper"/></param>
        /// <param name="downloadManager">Instance of <see cref="IDownloadManager"/></param>
        public EpisodesViewModel(
            ILogger logger,
            INavigationService navigationService,
            IApplicationSettingsHelper applicationSettingsHelper,
            IDownloadManager downloadManager,
            IEpisodeListManager episodeListManager)
        {
            this.logger = logger;
            this.navigationService = navigationService;
            this.applicationSettingsHelper = applicationSettingsHelper;
            this.downloadManager = downloadManager;
            this.episodeListManager = episodeListManager;

            RefreshCommand = new RelayCommand(loadEpisodeListFromServer);
            DownloadCommand = new RelayCommand(downloadEpisode);
            CancelDownloadCommand = new RelayCommand((Action<object>)cancelDownload);
            DeleteCommand = new RelayCommand((Action<object>)deleteEpisodeFromStorage);
            PlayCommand = new RelayCommand((Action<object>)playEpisode);
            AddToPlaylistCommand = new RelayCommand((Action<object>)addToPlaylistCommand);
            ClearPlaylistCommand = new RelayCommand((Action)clearPlaylistCommand);

            Application.Current.Resuming += onAppResuming;
            Application.Current.Suspending += (_, __) => unregisterUpdaterCompletionHandler();
            Application.Current.UnhandledException += (_, __) => unregisterUpdaterCompletionHandler();
            backgroundUpdaterTask = BackgroundTaskRegistration.AllTasks
                .Where(t => t.Value.Name == Constants.BackgroundUpdaterTaskName)
                .Select(t => t.Value)
                .FirstOrDefault();
            Initialization = initializeAsync();
            logger.LogMessage("EpisodesViewModel: Initialized.", LoggingLevel.Information);
        }

        private async Task initializeAsync()
        {
            await episodeListManager.Initialization;
            if (EpisodeList == null || !EpisodeList.Any())
            {
                logger.LogMessage("EpisodesViewModel: Saved list hasn't been found. Loading list from server.");
                await episodeListManager.LoadEpisodeListFromServerAsync();
            }
            else
            {
                logger.LogMessage("EpisodesViewModel: Loaded saved list from local storage. Updating episode states.");
                await episodeListManager.UpdateEpisodeStatesAsync();
            }

            await downloadManager.Initialization;
            registerUpdaterCompletionHandler();
            logger.LogMessage("EpisodesViewModel: State restored.", LoggingLevel.Information);
        }

        #endregion

        #region Commands

        private async Task loadEpisodeListFromServer() => await episodeListManager.LoadEpisodeListFromServerAsync();
        private async Task downloadEpisode(object boxedEpisode) => await downloadManager.DownloadEpisode(boxedEpisode as Episode);
        private void cancelDownload(object boxedEpisode) => downloadManager.CancelDownload(boxedEpisode as Episode);
        private async void deleteEpisodeFromStorage(object boxedEpisode) => await episodeListManager.DeleteEpisodeDataAsync(boxedEpisode as Episode);
        private async void addToPlaylistCommand(object boxedEpisode) => await episodeListManager.AddEpisodeToPLaylistAsync(boxedEpisode as Episode);

        private async void playEpisode(object boxedEpisode)
        {
            await episodeListManager.PlayEpisodeAsync(boxedEpisode as Episode);
            navigationService.Navigate(NavigationParameter.StartPlayback);
        }

        private async void clearPlaylistCommand()
        {
            logger.LogMessage("EpisodesViewModel: Executing ClearPlaylist command...");
            Playlist.Instance.Clear();
            await applicationSettingsHelper.SavePlaylistWithNotification();
            await episodeListManager.UpdateEpisodeStatesAsync();
            logger.LogMessage("EpisodesViewModel: ClearPlaylist command executed.");
        }

        #endregion

        #region Private Methods

        private async void onAppResuming(object sender, object e)
        {
            logger.LogMessage("EpisodesViewModel: Application is resuming. Restoring state...");
            await applicationSettingsHelper.LoadPlaylist();
            await episodeListManager.UpdateEpisodeStatesAsync();
            await downloadManager.RetrieveActiveDownloads();
            registerUpdaterCompletionHandler();
            logger.LogMessage("EpisodesViewModel: State restored after application resuming.", LoggingLevel.Information);
        }

        private async void backgroundUpdaterCompletionHandler(BackgroundTaskRegistration sender, BackgroundTaskCompletedEventArgs args)
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                await applicationSettingsHelper.LoadEpisodeList();
                await downloadManager.RetrieveActiveDownloads();
            });
        }

        private void unregisterUpdaterCompletionHandler()
        {
            if (backgroundUpdaterTask != null)
            {
                backgroundUpdaterTask.Completed -= backgroundUpdaterCompletionHandler;
                logger.LogMessage("EpisodesViewModel: Completion handler for background updater task unregistered.");
            }
        }

        private void registerUpdaterCompletionHandler()
        {
            if (backgroundUpdaterTask != null)
            {
                backgroundUpdaterTask.Completed -= backgroundUpdaterCompletionHandler;
                backgroundUpdaterTask.Completed += backgroundUpdaterCompletionHandler;
            }
            else
            {
                logger.LogMessage("EpisodesViewModel: Tried to register completion handler for background updater task, but no task was found.", LoggingLevel.Warning);
            }
        }

        #endregion
    }
}
