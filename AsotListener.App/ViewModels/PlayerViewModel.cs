namespace AsotListener.App.ViewModels
{
    using System;
    using System.Threading;
    using System.Windows.Input;
    using Models;
    using Services.Contracts;
    using Windows.Foundation.Collections;
    using Windows.Media.Playback;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Core;
    using Windows.UI.Xaml;
    using Windows.Foundation.Diagnostics;
    using Models.Enums;

    public class PlayerViewModel : BaseModel, IDisposable
    {
        #region Fields

        private MediaPlayer mediaPlayer;
        private bool isPreviousButtonEnabled;
        private bool isNextButtonEnabled;
        private bool isPlayButtonEnabled;
        private IconElement pauseIcon;
        private IconElement playIcon;
        private IconElement playButtonIcon;
        private bool isMyBackgroundTaskRunning = false;
        private IApplicationSettingsHelper applicationSettingsHelper;
        private AutoResetEvent backgroundAudioInitializedEvent;
        private ILogger logger;
        private INavigationService navigationService;

        #endregion

        #region Properties

        public bool IsPreviousButtonEnabled
        {
            get { return isPreviousButtonEnabled; }
            set { SetField(ref isPreviousButtonEnabled, value, nameof(IsPreviousButtonEnabled)); }
        }

        public bool IsNextButtonEnabled
        {
            get { return isNextButtonEnabled; }
            set { SetField(ref isNextButtonEnabled, value, nameof(IsNextButtonEnabled)); }
        }

        public bool IsPlayButtonEnabled
        {
            get { return isPlayButtonEnabled; }
            set { SetField(ref isPlayButtonEnabled, value, nameof(IsPlayButtonEnabled)); }
        }

        public IconElement PlayButtonIcon // TODO: Use two separate buttons for play and pause
        {
            get { return playButtonIcon; }
            set
            {
                value.Opacity = 1;
                SetField(ref playButtonIcon, value, nameof(PlayButtonIcon));
            }
        }

        public ICommand PreviousTrackCommand { get; }
        public ICommand NextTrackCommand { get; }
        public ICommand PlayPauseCommand { get; }

        public IPlayList Playlist { get; }

        #endregion

        #region Ctor

        public PlayerViewModel(
            ILogger logger, 
            IPlayList playlist, 
            IApplicationSettingsHelper applicationSettingsHelper,
            INavigationService navigationService)
        {
            backgroundAudioInitializedEvent = new AutoResetEvent(false);
            this.logger = logger;
            this.navigationService = navigationService;

            Playlist = playlist;
            this.applicationSettingsHelper = applicationSettingsHelper;
            mediaPlayer = BackgroundMediaPlayer.Current;

            pauseIcon = new SymbolIcon(Symbol.Pause);
            playIcon = new SymbolIcon(Symbol.Play);
            PlayButtonIcon = playIcon;

            // Using explicit casts here because of http://stackoverflow.com/questions/2057146/compiler-ambiguous-invocation-error-anonymous-method-and-method-group-with-fun
            PreviousTrackCommand = new RelayCommand((Action)onPreviousTrackAction);
            NextTrackCommand = new RelayCommand((Action)onNextButtonAction);
            PlayPauseCommand = new RelayCommand((Action)onPlayPauseAction);

            updateBackgroundTaskRunningStatus();

            //Adding App suspension handlers here so that we can unsubscribe handlers 
            //that access to BackgroundMediaPlayer events
            Application.Current.Suspending += ForegroundApp_Suspending;
            Application.Current.Resuming += ForegroundApp_Resuming;

            initializeAsync();
        }

        private async void initializeAsync()
        {
            Frame rootFrame = Window.Current.Content as Frame;

            if (isMyBackgroundTaskRunning)
            {
                // Playlist has been already loaded
                navigationService.Navigate(NavigationParameter.OpenPlayer);
                return;
            }

            await Playlist.LoadPlaylistFromLocalStorage();
            if (Playlist.CurrentTrack == null)
            {
                return;
            }

            navigationService.Navigate(NavigationParameter.OpenPlayer);

            logger.LogMessage("Foreground audio player initialized.");
        }

        #endregion

        #region Foreground App Lifecycle Handlers

        /// <summary>
        /// Sends message to background informing app has resumed
        /// Subscribe to MediaPlayer events
        /// </summary>
        void ForegroundApp_Resuming(object sender, object e)
        {
            logger.LogMessage("Foreground audio player resuming...");
            applicationSettingsHelper.SaveSettingsValue(Constants.AppState, ForegroundAppStatus.Active.ToString());

            // Verify if the task was running before
            updateBackgroundTaskRunningStatus();
            if (isMyBackgroundTaskRunning)
            {
                //if yes, reconnect to media play handlers
                AddMediaPlayerEventHandlers();

                //send message to background task that app is resumed, so it can start sending notifications
                ValueSet message = new ValueSet() { { Constants.AppResumed, DateTime.Now.ToString() } };
                BackgroundMediaPlayer.SendMessageToBackground(message);

                if (mediaPlayer.CurrentState == MediaPlayerState.Playing)
                {
                    PlayButtonIcon = pauseIcon;
                }
                else
                {
                    PlayButtonIcon = playIcon;
                }
            }
            else
            {
                PlayButtonIcon = playIcon;
            }

            logger.LogMessage("Foreground audio player resumed.");
        }

        /// <summary>
        /// Send message to Background process that app is to be suspended
        /// Unsubscribe handlers for MediaPlayer events
        /// </summary>
        void ForegroundApp_Suspending(object sender, Windows.ApplicationModel.SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            logger.LogMessage("Foreground audio player suspending...");
            ValueSet message = new ValueSet() { { Constants.AppSuspended, DateTime.Now.ToString() } };
            BackgroundMediaPlayer.SendMessageToBackground(message);
            RemoveMediaPlayerEventHandlers();
            applicationSettingsHelper.SaveSettingsValue(Constants.AppState, ForegroundAppStatus.Suspended.ToString());
            logger.LogMessage("Foreground audio player suspended.");
            deferral.Complete();
        }

        #endregion

        #region Button Commands

        /// <summary>
        /// Sends message to the background task to skip to the previous track.
        /// </summary>
        private void onPreviousTrackAction()
        {
            logger.LogMessage("Foreground audio player 'Previous Track' command fired.");
            var value = new ValueSet() { { Constants.SkipPrevious, string.Empty } };
            BackgroundMediaPlayer.SendMessageToBackground(value);

            // Prevent the user from repeatedly pressing the button and causing 
            // a back-long of button presses to be handled. This button is re-enabled 
            // in the TrackReady Play state handler.
            IsPreviousButtonEnabled = false;
        }

        /// <summary>
        /// If the task is already running, it will just play/pause MediaPlayer Instance
        /// Otherwise, initializes MediaPlayer Handlers and starts playback
        /// track or to pause if we're already playing.
        /// </summary>
        private void onPlayPauseAction()
        {
            logger.LogMessage("Foreground audio player 'Play/Pause' command fired.");

            // Play button will be enabled when media player will be ready
            IsPlayButtonEnabled = false;
            updateBackgroundTaskRunningStatus();
            if (isMyBackgroundTaskRunning)
            {
                if (MediaPlayerState.Playing == BackgroundMediaPlayer.Current.CurrentState)
                {
                    var message = new ValueSet() { { Constants.PausePlayback, string.Empty } };
                    BackgroundMediaPlayer.SendMessageToBackground(message);
                }
                else if (MediaPlayerState.Paused == BackgroundMediaPlayer.Current.CurrentState)
                {
                    var message = new ValueSet() { { Constants.StartPlayback, string.Empty } };
                    BackgroundMediaPlayer.SendMessageToBackground(message);
                }
                else if (MediaPlayerState.Closed == BackgroundMediaPlayer.Current.CurrentState)
                {
                    StartBackgroundAudioTask();
                }
            }
            else
            {
                StartBackgroundAudioTask();
            }
        }

        /// <summary>
        /// Tells the background audio agent to skip to the next track.
        /// </summary>
        private void onNextButtonAction()
        {
            logger.LogMessage("Foreground audio player 'Next Track' command fired.");

            var value = new ValueSet() { { Constants.SkipNext, string.Empty } };
            BackgroundMediaPlayer.SendMessageToBackground(value);

            // Prevent the user from repeatedly pressing the button and causing 
            // a back-long of button presses to be handled. This button is re-enabled 
            // in the TrackReady Play-state handler.
            IsNextButtonEnabled = false;
        }

        #endregion

        #region Background MediaPlayer Event handlers

        /// <summary>
        /// MediaPlayer state changed event handlers. 
        /// Note that we can subscribe to events even if Media Player is playing media in background
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private async void MediaPlayer_CurrentStateChanged(MediaPlayer sender, object args)
        {
            logger.LogMessage("Foreground audio player 'MediaPlayer_CurrentStateChanged' event fired.");

            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                IsPlayButtonEnabled = true;
                switch (sender.CurrentState)
                {
                    case MediaPlayerState.Playing:
                        PlayButtonIcon = pauseIcon;
                        IsNextButtonEnabled = true;
                        IsPreviousButtonEnabled = true;
                        break;
                    case MediaPlayerState.Paused:
                        PlayButtonIcon = playIcon;
                        break;
                }
            });
        }

        /// <summary>
        /// This event fired when a message is received from Background Process
        /// </summary>
        void BackgroundMediaPlayer_MessageReceivedFromBackground(object sender, MediaPlayerDataReceivedEventArgs e)
        {
            foreach (string key in e.Data.Keys)
            {
                if (key == Constants.BackgroundTaskStarted)
                {
                    logger.LogMessage("Foreground audio player MessageReceivedFromBackground: Background Task started.");
                    backgroundAudioInitializedEvent.Set();
                    return;
                }
            }
        }

        #endregion

        #region Helper Methods
        
        private void updateBackgroundTaskRunningStatus()
        {
            string taskState = applicationSettingsHelper.ReadSettingsValue<string>(Constants.BackgroundTaskState);
            isMyBackgroundTaskRunning = taskState == Constants.BackgroundTaskRunning;
        }

        /// <summary>
        /// Unsubscribes to MediaPlayer events. Should run only on suspend
        /// </summary>
        private void RemoveMediaPlayerEventHandlers()
        {
            BackgroundMediaPlayer.Current.CurrentStateChanged -= this.MediaPlayer_CurrentStateChanged;
            BackgroundMediaPlayer.MessageReceivedFromBackground -= this.BackgroundMediaPlayer_MessageReceivedFromBackground;
        }

        /// <summary>
        /// Subscribes to MediaPlayer events
        /// </summary>
        private void AddMediaPlayerEventHandlers()
        {
            BackgroundMediaPlayer.Current.CurrentStateChanged += MediaPlayer_CurrentStateChanged;
            BackgroundMediaPlayer.MessageReceivedFromBackground += BackgroundMediaPlayer_MessageReceivedFromBackground;
        }

        /// <summary>
        /// Initialize Background Media Player Handlers and starts playback
        /// </summary>
        private void StartBackgroundAudioTask()
        {
            logger.LogMessage("Foreground audio player Starting Background Task...");
            AddMediaPlayerEventHandlers();

            updateBackgroundTaskRunningStatus();
            if (isMyBackgroundTaskRunning)
            {
                backgroundAudioInitializedEvent.Set();
            }

            bool result = backgroundAudioInitializedEvent.WaitOne(Constants.BackgroundAudioWaitingTime);
            if (result == true)
            {
                var message = new ValueSet() { { Constants.StartPlayback, string.Empty } };
                BackgroundMediaPlayer.SendMessageToBackground(message);
            }
            else
            {
                var message = "Background Audio Task didn't start in expected time";
                logger.LogMessage(message, LoggingLevel.Error);
                throw new Exception(message);
            }
        }

        #endregion

        #region IDisposable Support

        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    this.backgroundAudioInitializedEvent.Dispose();
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
