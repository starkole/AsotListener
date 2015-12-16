namespace AsotListener.App.ViewModels
{
    using System;
    using System.Threading;
    using System.Windows.Input;
    using Models;
    using System.Linq;
    using Services.Contracts;
    using Windows.Foundation.Collections;
    using Windows.Media.Playback;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Core;
    using Windows.UI.Xaml;
    using Windows.Foundation.Diagnostics;
    using Models.Enums;

    public sealed class PlayerViewModel : BaseModel, IDisposable
    {
        #region Fields

        private MediaPlayer mediaPlayer;
        private bool isPreviousButtonEnabled;
        private bool isNextButtonEnabled;
        private bool isPlayButtonEnabled;
        private IconElement pauseIcon;
        private IconElement playIcon;
        private IconElement playButtonIcon;
        private IApplicationSettingsHelper applicationSettingsHelper;
        private AutoResetEvent backgroundAudioInitializedEvent;
        private ILogger logger;
        private INavigationService navigationService;

        #endregion

        #region Properties
        private bool IsBackgroundTaskRunning => applicationSettingsHelper.ReadSettingsValue<bool>(Constants.IsBackgroundTaskRunning);

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
            INavigationService navigationService,
            MediaPlayer mediaPlayer)
        {
            backgroundAudioInitializedEvent = new AutoResetEvent(false);
            this.logger = logger;
            this.navigationService = navigationService;

            Playlist = playlist;
            this.applicationSettingsHelper = applicationSettingsHelper;
            this.mediaPlayer = mediaPlayer;

            pauseIcon = new SymbolIcon(Symbol.Pause);
            playIcon = new SymbolIcon(Symbol.Play);
            PlayButtonIcon = playIcon;

            // Using explicit casts here because of http://stackoverflow.com/questions/2057146/compiler-ambiguous-invocation-error-anonymous-method-and-method-group-with-fun
            PreviousTrackCommand = new RelayCommand((Action)onPreviousTrackAction);
            NextTrackCommand = new RelayCommand((Action)onNextTrackAction);
            PlayPauseCommand = new RelayCommand((Action)onPlayPauseAction);

            Application.Current.Suspending += ForegroundApp_Suspending;
            Application.Current.Resuming += ForegroundApp_Resuming;

            initializeAsync();
        }

        private async void initializeAsync()
        {
            await Playlist.LoadPlaylistFromLocalStorage();
            if (Playlist.CurrentTrack != null)
            {
                navigationService.Navigate(NavigationParameter.OpenPlayer);
                IsNextButtonEnabled = true;
                IsPlayButtonEnabled = true;
                IsPreviousButtonEnabled = true;
            }

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
            if (IsBackgroundTaskRunning)
            {
                AddMediaPlayerEventHandlers();
                PlayButtonIcon = mediaPlayer.CurrentState == MediaPlayerState.Playing ?
                    pauseIcon :
                    playIcon;
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
            RemoveMediaPlayerEventHandlers();
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

            if (IsBackgroundTaskRunning &&
                mediaPlayer.CurrentState == MediaPlayerState.Playing)
            {
                var message = new ValueSet() { { Constants.PausePlayback, string.Empty } };
                BackgroundMediaPlayer.SendMessageToBackground(message);
                return;
            }

            WaitForBackgroundAudioTask();
        }

        /// <summary>
        /// Tells the background audio agent to skip to the next track.
        /// </summary>
        private void onNextTrackAction()
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

        #region MediaPlayer Event handlers

        /// <summary>
        /// MediaPlayer state changed event handlers. 
        /// Note that we can subscribe to events even if Media Player is playing media in background
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private async void onMediaPlayerCurrentStateChanged(MediaPlayer sender, object args)
        {
            logger.LogMessage("Foreground audio player 'MediaPlayer_CurrentStateChanged' event fired.");

            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                if (sender.CurrentState == MediaPlayerState.Playing)
                {
                    IsPlayButtonEnabled = true;
                    IsNextButtonEnabled = true;
                    IsPreviousButtonEnabled = true;
                    PlayButtonIcon = pauseIcon;
                }

                if (sender.CurrentState == MediaPlayerState.Paused)
                {
                    IsPlayButtonEnabled = true;
                    PlayButtonIcon = playIcon;
                }
            });
        }

        /// <summary>
        /// This event fired when a message is received from Background Process
        /// </summary>
        void onMessageReceivedFromBackground(object sender, MediaPlayerDataReceivedEventArgs e)
        {
            if (e.Data.ContainsKey(Constants.IsBackgroundTaskRunning))
            {
                logger.LogMessage("Foreground audio player MessageReceivedFromBackground: Background Task started.");
                backgroundAudioInitializedEvent.Set();
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Unsubscribes to MediaPlayer events. Should run only on suspend
        /// </summary>
        private void RemoveMediaPlayerEventHandlers()
        {
            mediaPlayer.CurrentStateChanged -= onMediaPlayerCurrentStateChanged;
            BackgroundMediaPlayer.MessageReceivedFromBackground -= onMessageReceivedFromBackground;
        }

        /// <summary>
        /// Subscribes to MediaPlayer events
        /// </summary>
        private void AddMediaPlayerEventHandlers()
        {
            mediaPlayer.CurrentStateChanged += onMediaPlayerCurrentStateChanged;
            BackgroundMediaPlayer.MessageReceivedFromBackground += onMessageReceivedFromBackground;
        }

        /// <summary>
        /// Initialize Background Media Player Handlers and starts playback
        /// </summary>
        private void WaitForBackgroundAudioTask()
        {
            logger.LogMessage("Foreground audio player: waiting for Background Task...");
            AddMediaPlayerEventHandlers();
            if (IsBackgroundTaskRunning)
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
                var message = "Foreground audio player: Background Audio Task didn't start in expected time";
                logger.LogMessage(message, LoggingLevel.Error);
                throw new Exception(message);
            }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            backgroundAudioInitializedEvent.Dispose();
        }

        #endregion
    }
}
