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

    public sealed class PlayerViewModel : BaseModel, IDisposable
    {
        #region Fields

        private readonly DispatcherTimer progressUpdateTimer;
        private bool isAudioSeekerEnabled = false;
        private double audioSeekerStepFrequency = 1;
        private double audioSeekerValue = 0;
        private double audioSeekerMaximum = 2;
        private string currentTrackPlayedTime;
        private string currentTrackLeftToplay;
        private readonly MediaPlayer mediaPlayer;
        private bool isPreviousButtonEnabled;
        private bool isNextButtonEnabled;
        private bool isPlayButtonEnabled;
        private readonly IconElement pauseIcon;
        private readonly IconElement playIcon;
        private IconElement playButtonIcon;
        private readonly IApplicationSettingsHelper applicationSettingsHelper;
        private readonly AutoResetEvent backgroundAudioInitializedEvent;
        private readonly ILogger logger;
        private readonly INavigationService navigationService;

        #endregion

        #region Properties

        private bool IsBackgroundTaskRunning => applicationSettingsHelper.ReadSettingsValue<bool>(Constants.IsBackgroundTaskRunning);

        public bool IsAudioSeekerEnabled
        {
            get { return isAudioSeekerEnabled; }
            set { SetField(ref isAudioSeekerEnabled, value, nameof(IsAudioSeekerEnabled)); }
        }

        public double AudioSeekerStepFrequency
        {
            get { return audioSeekerStepFrequency; }
            set
            { SetField(ref audioSeekerStepFrequency, value, nameof(AudioSeekerStepFrequency)); }
        }

        public double AudioSeekerValue
        {
            get { return audioSeekerValue; }
            set
            { SetField(ref audioSeekerValue, value, nameof(AudioSeekerValue)); }
        }

        public double AudioSeekerMaximum
        {
            get { return audioSeekerMaximum; }
            set { SetField(ref audioSeekerMaximum, value, nameof(AudioSeekerMaximum)); }
        }

        public bool CanUpdateAudioSeeker { get; set; } = true;

        public string CurrentTrackPlayedTime
        {
            get { return currentTrackPlayedTime; }
            set { SetField(ref currentTrackPlayedTime, value, nameof(CurrentTrackPlayedTime)); }
        }

        public string CurrentTrackLeftToplay
        {
            get { return currentTrackLeftToplay; }
            set { SetField(ref currentTrackLeftToplay, value, nameof(CurrentTrackLeftToplay)); }
        }

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

        public IconElement PlayButtonIcon
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
            Playlist = playlist;
            this.logger = logger;
            this.navigationService = navigationService;
            this.applicationSettingsHelper = applicationSettingsHelper;
            this.mediaPlayer = mediaPlayer;
            pauseIcon = new SymbolIcon(Symbol.Pause);
            playIcon = new SymbolIcon(Symbol.Play);
            PlayButtonIcon = playIcon;
            progressUpdateTimer = new DispatcherTimer();

            // Using explicit casts here because of http://stackoverflow.com/questions/2057146/compiler-ambiguous-invocation-error-anonymous-method-and-method-group-with-fun
            PreviousTrackCommand = new RelayCommand((Action)onPreviousTrackAction);
            NextTrackCommand = new RelayCommand((Action)onNextTrackAction);
            PlayPauseCommand = new RelayCommand((Action)onPlayPauseAction);

            Application.Current.Suspending += onAppSuspending;
            Application.Current.Resuming += initializeAsync;

            initializeAsync(null, null);
            logger.LogMessage("Foreground audio player initialized.");
        }

        #endregion

        #region Foreground App Lifecycle Handlers

        /// <summary>
        /// Sends message to background informing app has resumed
        /// Subscribe to MediaPlayer events
        /// </summary>
        private async void initializeAsync(object sender, object e)
        {
            logger.LogMessage("Foreground audio player updating current state...");
            await Playlist.LoadPlaylistFromLocalStorage();
            if (Playlist.CurrentTrack != null)
            {
                navigationService.Navigate(NavigationParameter.OpenPlayer);
                IsNextButtonEnabled = true;
                IsPlayButtonEnabled = true;
                IsPreviousButtonEnabled = true;
                setupAudioProgress();
            }

            if (IsBackgroundTaskRunning)
            {
                AddMediaPlayerEventHandlers();
                if (mediaPlayer.CurrentState == MediaPlayerState.Playing)
                {
                    PlayButtonIcon = pauseIcon;
                    startProgressUpdateTimer();
                } else
                {
                    PlayButtonIcon = playIcon;
                }
            }
            else
            {
                PlayButtonIcon = playIcon;
            }

            logger.LogMessage("Foreground audio player current state updated.");
        }

        private async void onAppSuspending(object sender, Windows.ApplicationModel.SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            RemoveMediaPlayerEventHandlers();
            if (mediaPlayer.Position != TimeSpan.Zero)
            {
                Playlist.CurrentTrack.StartPosition = mediaPlayer.Position;
            }

            await Playlist.SavePlaylistToLocalStorage();
            logger.LogMessage("Foreground audio player suspended.");
            deferral.Complete();
        }

        #endregion

        #region Button Commands

        private void onPreviousTrackAction()
        {
            logger.LogMessage("Foreground audio player 'Previous Track' command fired.");
            var value = new ValueSet { { Constants.SkipPrevious, string.Empty } };
            BackgroundMediaPlayer.SendMessageToBackground(value);

            // Prevent the user from repeatedly pressing the button and causing 
            // a back-long of button presses to be handled. This button is re-enabled 
            // in the TrackReady Play state handler.
            IsPreviousButtonEnabled = false;
        }

        private void onPlayPauseAction()
        {
            logger.LogMessage("Foreground audio player 'Play/Pause' command fired.");

            // Play button will be enabled when media player will be ready
            IsPlayButtonEnabled = false;

            if (IsBackgroundTaskRunning &&
                mediaPlayer.CurrentState == MediaPlayerState.Playing)
            {
                var message = new ValueSet { { Constants.PausePlayback, string.Empty } };
                BackgroundMediaPlayer.SendMessageToBackground(message);
                return;
            }

            WaitForBackgroundAudioTask();
        }

        private void onNextTrackAction()
        {
            logger.LogMessage("Foreground audio player 'Next Track' command fired.");

            var value = new ValueSet { { Constants.SkipNext, string.Empty } };
            BackgroundMediaPlayer.SendMessageToBackground(value);

            // Prevent the user from repeatedly pressing the button and causing 
            // a back-long of button presses to be handled. This button is re-enabled 
            // in the TrackReady Play-state handler.
            IsNextButtonEnabled = false;
        }

        #endregion

        #region MediaPlayer Event handlers

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
                    IsAudioSeekerEnabled = true;
                    PlayButtonIcon = pauseIcon;
                    startProgressUpdateTimer();
                }

                if (sender.CurrentState == MediaPlayerState.Paused)
                {
                    IsPlayButtonEnabled = true;
                    PlayButtonIcon = playIcon;
                    stopProgressUpdateTimer();
                }
            });
        }

        void onMessageReceivedFromBackground(object sender, MediaPlayerDataReceivedEventArgs e)
        {
            if (e.Data.ContainsKey(Constants.IsBackgroundTaskRunning))
            {
                logger.LogMessage("Foreground audio player MessageReceivedFromBackground: Background Task started.");
                backgroundAudioInitializedEvent.Set();
            }
        }

        #endregion

        #region Progress Tracking

        private int getAudioSeekerStepFrequency(TimeSpan timevalue)
        {
            if (timevalue.TotalHours >= 1)
            {
                return 60;
            }

            if (timevalue.TotalMinutes >= 10 && timevalue.TotalMinutes < 30)
            {
                return 10;
            }

            if (timevalue.TotalMinutes >= 30 && timevalue.TotalMinutes < 60)
            {
                return 30;
            }

            return 1;
        }

        private void setupAudioProgress()
        {
            TimeSpan overallDuration;
            try
            {
                overallDuration = mediaPlayer.NaturalDuration <= TimeSpan.Zero ? 
                    TimeSpan.Zero : 
                    mediaPlayer.NaturalDuration;
            }
            catch
            {
                overallDuration = TimeSpan.Zero;
            }
            AudioSeekerStepFrequency = getAudioSeekerStepFrequency(overallDuration);
            logger.LogMessage("Foreground audio player: Starting progress update timer...");
            
            CurrentTrackLeftToplay = "-" + (overallDuration - mediaPlayer.Position).ToUserFriendlyString();
            CurrentTrackPlayedTime = mediaPlayer.Position.ToUserFriendlyString();
            AudioSeekerMaximum = Math.Round(overallDuration.TotalSeconds) + 1;
            AudioSeekerValue = Math.Round(mediaPlayer.Position.TotalSeconds);
        }

        private void startProgressUpdateTimer()
        {
            setupAudioProgress();
            IsAudioSeekerEnabled = true;
            progressUpdateTimer.Tick += onTimerTick;
            progressUpdateTimer.Start();
            logger.LogMessage($"Foreground audio player: Progress update timer started with interval {progressUpdateTimer.Interval}.");
        }

        private void stopProgressUpdateTimer()
        {
            logger.LogMessage("Foreground audio player: Stopping progress update timer...");
            if (progressUpdateTimer.IsEnabled)
            {
                progressUpdateTimer.Stop();
                progressUpdateTimer.Tick -= onTimerTick;
                logger.LogMessage("Foreground audio player: Progress update timer stopped.");
            }
        }

        private void onTimerTick(object sender, object e)
        {
            if (CanUpdateAudioSeeker)
            {
                AudioSeekerValue = Math.Round(mediaPlayer.Position.TotalSeconds);
                CurrentTrackLeftToplay = "-" + (mediaPlayer.NaturalDuration - mediaPlayer.Position).ToUserFriendlyString();
                CurrentTrackPlayedTime = mediaPlayer.Position.ToUserFriendlyString();
            }
        }

        #endregion

        #region Helper Methods

        private void RemoveMediaPlayerEventHandlers()
        {
            mediaPlayer.CurrentStateChanged -= onMediaPlayerCurrentStateChanged;
            BackgroundMediaPlayer.MessageReceivedFromBackground -= onMessageReceivedFromBackground;
        }

        private void AddMediaPlayerEventHandlers()
        {
            mediaPlayer.CurrentStateChanged += onMediaPlayerCurrentStateChanged;
            BackgroundMediaPlayer.MessageReceivedFromBackground += onMessageReceivedFromBackground;
        }

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
                logger.LogMessage("Foreground audio player: Background Task is running. Sending play command.");
                var message = new ValueSet { { Constants.StartPlayback, string.Empty } };
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
            logger.LogMessage("Foreground audio player: disposing.");
            stopProgressUpdateTimer();
            backgroundAudioInitializedEvent.Dispose();
        }

        #endregion
    }
}
