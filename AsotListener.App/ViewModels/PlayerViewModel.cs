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
    using Common;
    using System.Threading.Tasks;
    /// <summary>
    /// View model of audio player
    /// </summary>
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
        private bool isPreviousButtonEnabled;
        private bool isNextButtonEnabled;
        private bool isPlayButtonEnabled;
        private readonly IconElement pauseIcon;
        private readonly IconElement playIcon;
        private IconElement playButtonIcon;
        private string currentTrackName;
        private readonly IApplicationSettingsHelper applicationSettingsHelper;
        private readonly AutoResetEvent backgroundAudioInitializedEvent;
        private const int backgroundAudioWaitingTime = 2000; // 2 sec.
        private readonly ILogger logger;
        private readonly INavigationService navigationService;

        #endregion

        #region Properties

        private MediaPlayer MediaPlayer => BackgroundMediaPlayer.Current;
        private bool IsBackgroundTaskRunning => applicationSettingsHelper.ReadSettingsValue<bool>(Keys.IsBackgroundTaskRunning);

        /// <summary>
        /// Determines if audio seeker slider must be enabled
        /// </summary>
        public bool IsAudioSeekerEnabled
        {
            get { return isAudioSeekerEnabled; }
            set { SetField(ref isAudioSeekerEnabled, value, nameof(IsAudioSeekerEnabled)); }
        }

        /// <summary>
        /// Step frequency for audio seeker slider
        /// </summary>
        public double AudioSeekerStepFrequency
        {
            get { return audioSeekerStepFrequency; }
            set { SetField(ref audioSeekerStepFrequency, value, nameof(AudioSeekerStepFrequency)); }
        }

        /// <summary>
        /// Current value of audio seeker slider
        /// </summary>
        public double AudioSeekerValue
        {
            get { return audioSeekerValue; }
            set { SetField(ref audioSeekerValue, value, nameof(AudioSeekerValue)); }
        }

        /// <summary>
        /// Maximum value of audio seeker slider
        /// </summary>
        public double AudioSeekerMaximum
        {
            get { return audioSeekerMaximum; }
            set { SetField(ref audioSeekerMaximum, value, nameof(AudioSeekerMaximum)); }
        }

        /// <summary>
        /// Determines if audio seeker slider value can be updated
        /// </summary>
        public bool CanUpdateAudioSeeker { get; set; } = true;

        /// <summary>
        /// Played time of current track as formatted string
        /// </summary>
        public string CurrentTrackPlayedTime
        {
            get { return currentTrackPlayedTime; }
            set { SetField(ref currentTrackPlayedTime, value, nameof(CurrentTrackPlayedTime)); }
        }

        /// <summary>
        /// Current track time left to play as formatted string
        /// </summary>
        public string CurrentTrackLeftToplay
        {
            get { return currentTrackLeftToplay; }
            set { SetField(ref currentTrackLeftToplay, value, nameof(CurrentTrackLeftToplay)); }
        }

        /// <summary>
        /// Determines if "Previous" button must be enabled
        /// </summary>
        public bool IsPreviousButtonEnabled
        {
            get { return isPreviousButtonEnabled; }
            set { SetField(ref isPreviousButtonEnabled, value, nameof(IsPreviousButtonEnabled)); }
        }

        /// <summary>
        /// Determines if "Next" button must be enabled
        /// </summary>
        public bool IsNextButtonEnabled
        {
            get { return isNextButtonEnabled; }
            set { SetField(ref isNextButtonEnabled, value, nameof(IsNextButtonEnabled)); }
        }

        /// <summary>
        /// Determines if "Play" button must be enabled
        /// </summary>
        public bool IsPlayButtonEnabled
        {
            get { return isPlayButtonEnabled; }
            set { SetField(ref isPlayButtonEnabled, value, nameof(IsPlayButtonEnabled)); }
        }

        /// <summary>
        /// Current "Play" button icon
        /// </summary>
        public IconElement PlayButtonIcon
        {
            get { return playButtonIcon; }
            set
            {
                value.Opacity = 1;
                SetField(ref playButtonIcon, value, nameof(PlayButtonIcon));
            }
        }

        /// <summary>
        /// Returns to previous track
        /// </summary>
        public ICommand PreviousTrackCommand { get; }

        /// <summary>
        /// Advances to the next track
        /// </summary>
        public ICommand NextTrackCommand { get; }

        /// <summary>
        /// Starts playback or pauses player depending on its state
        /// </summary>
        public ICommand PlayPauseCommand { get; }

        /// <summary>
        /// The name of the current track
        /// </summary>
        public string CurrentTrackName
        {
            get { return currentTrackName; }
            set { SetField(ref currentTrackName, value, nameof(CurrentTrackName)); }
        }

        #endregion

        #region Ctor

        /// <summary>
        /// Creates instance of <see cref="PlayerViewModel"/>
        /// </summary>
        /// <param name="logger">Instance of <see cref="ILogger"/></param>
        /// <param name="playlist">Instance of <see cref="IPlayList"/></param>
        /// <param name="applicationSettingsHelper">Instance of <see cref="IApplicationSettingsHelper"/></param>
        /// <param name="navigationService">Instance of <see cref="INavigationService"/></param>
        public PlayerViewModel(
            ILogger logger,
            IApplicationSettingsHelper applicationSettingsHelper,
            INavigationService navigationService)
        {
            backgroundAudioInitializedEvent = new AutoResetEvent(false);
            this.logger = logger;
            this.navigationService = navigationService;
            this.applicationSettingsHelper = applicationSettingsHelper;
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

            // Ensure that Background Audio is initialized by accessing BackgroundMediaPlayer.Current
            var state = MediaPlayer.CurrentState;
            logger.LogMessage($"Foreground audio: Current media player state is {state}.", LoggingLevel.Information);

            initializeAsync(null, null);
            logger.LogMessage("Foreground audio player initialized.", LoggingLevel.Information);
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

            await applicationSettingsHelper.LoadPlaylist();
            if (Playlist.Instance.CurrentTrack != null)
            {
                navigationService.Navigate(NavigationParameter.OpenPlayer);
                IsNextButtonEnabled = true;
                IsPlayButtonEnabled = true;
                IsPreviousButtonEnabled = true;
                await setupAudioProgress();
            }

            if (IsBackgroundTaskRunning)
            {
                AddMediaPlayerEventHandlers();
                if (MediaPlayer.CurrentState == MediaPlayerState.Playing)
                {
                    PlayButtonIcon = pauseIcon;
                    await setupAudioProgress();
                    startProgressUpdateTimer();
                }
                else if (MediaPlayer.CurrentState == MediaPlayerState.Paused)
                {
                    PlayButtonIcon = playIcon;
                    await setupAudioProgress();
                    startProgressUpdateTimer();
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

            logger.LogMessage("Foreground audio player current state updated.");
        }

        private void onAppSuspending(object sender, Windows.ApplicationModel.SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            RemoveMediaPlayerEventHandlers();            
            logger.LogMessage("Foreground audio player suspended.", LoggingLevel.Information);
            deferral.Complete();
        }

        #endregion

        #region Button Commands

        private void onPreviousTrackAction()
        {
            logger.LogMessage("Foreground audio player 'Previous Track' command fired.");
            var value = new ValueSet { { Keys.SkipToPrevious, string.Empty } };
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
                MediaPlayer.CurrentState == MediaPlayerState.Playing)
            {
                var message = new ValueSet { { Keys.PausePlayback, string.Empty } };
                BackgroundMediaPlayer.SendMessageToBackground(message);
                return;
            }

            WaitForBackgroundAudioTask();
        }

        private void onNextTrackAction()
        {
            logger.LogMessage("Foreground audio player 'Next Track' command fired.");

            var value = new ValueSet { { Keys.SkipToNext, string.Empty } };
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
            logger.LogMessage($"Foreground audio player 'MediaPlayer_CurrentStateChanged' event fired with status {sender.CurrentState}.");

            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                if (sender.CurrentState == MediaPlayerState.Opening)
                {
                    stopProgressUpdateTimer();
                    await setupAudioProgress();
                }

                if (sender.CurrentState == MediaPlayerState.Playing)
                {
                    IsPlayButtonEnabled = true;
                    IsNextButtonEnabled = true;
                    IsPreviousButtonEnabled = true;
                    IsAudioSeekerEnabled = true;
                    PlayButtonIcon = pauseIcon;
                    await setupAudioProgress();
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

        private void onMessageReceivedFromBackground(object sender, MediaPlayerDataReceivedEventArgs e)
        {
            if (e.Data.ContainsKey(Keys.IsBackgroundTaskRunning))
            {
                logger.LogMessage("Foreground audio player MessageReceivedFromBackground: Background Task started.");
                backgroundAudioInitializedEvent.Set();
            }
        }

        #endregion

        #region Progress Tracking

        /// <summary>
        /// Changes current player position based on audio seeker slider value changes
        /// </summary>
        /// <param name="slider">Slider instance</param>
        public void UpdateProgressFromSlider(Slider slider)
        {
            if (BackgroundMediaPlayer.Current.CurrentState == MediaPlayerState.Playing && slider != null)
            {
                var newPosition = slider.Value < 0 ? 0 : slider.Value;
                var totalSeconds = Math.Round(BackgroundMediaPlayer.Current.NaturalDuration.TotalSeconds) - 1;
                newPosition = newPosition > totalSeconds ? totalSeconds : newPosition;
                BackgroundMediaPlayer.Current.Position = TimeSpan.FromSeconds(newPosition);
                logger.LogMessage($"Foreground audio player: Player position updated to {newPosition} seconds.");
            }
        }

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

        private async Task setupAudioProgress()
        {
            await applicationSettingsHelper.LoadPlaylist();
            TimeSpan overallDuration = MediaPlayer.NaturalDuration <= TimeSpan.Zero ?
                    TimeSpan.Zero :
                    MediaPlayer.NaturalDuration;
            TimeSpan currentPosition = MediaPlayer.Position;
            AudioSeekerStepFrequency = getAudioSeekerStepFrequency(overallDuration);
            logger.LogMessage("Foreground audio player: Starting progress update timer...");

            CurrentTrackLeftToplay = (overallDuration - currentPosition).ToUserFriendlyString();
            CurrentTrackPlayedTime = currentPosition.ToUserFriendlyString();
            AudioSeekerMaximum = Math.Round(overallDuration.TotalSeconds) + 1;
            AudioSeekerValue = Math.Round(currentPosition.TotalSeconds);
            CurrentTrackName = Playlist.Instance.CurrentTrack.Name;
        }

        private void startProgressUpdateTimer()
        {
            IsAudioSeekerEnabled = true;
            progressUpdateTimer.Tick += onTimerTick;
            progressUpdateTimer.Start();
            logger.LogMessage($"Foreground audio player: Progress update timer started with interval {progressUpdateTimer.Interval}.", LoggingLevel.Information);
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
                AudioSeekerValue = Math.Round(MediaPlayer.Position.TotalSeconds);
                CurrentTrackLeftToplay = (MediaPlayer.NaturalDuration - MediaPlayer.Position).ToUserFriendlyString();
                CurrentTrackPlayedTime = MediaPlayer.Position.ToUserFriendlyString();
            }
        }

        #endregion

        #region Helper Methods

        private void RemoveMediaPlayerEventHandlers()
        {
            MediaPlayer.CurrentStateChanged -= onMediaPlayerCurrentStateChanged;
            BackgroundMediaPlayer.MessageReceivedFromBackground -= onMessageReceivedFromBackground;
        }

        private void AddMediaPlayerEventHandlers()
        {
            MediaPlayer.CurrentStateChanged += onMediaPlayerCurrentStateChanged;
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

            bool result = backgroundAudioInitializedEvent.WaitOne(backgroundAudioWaitingTime);
            if (result == true)
            {
                logger.LogMessage("Foreground audio player: Background Task is running. Sending play command.");
                var message = new ValueSet { { Keys.StartPlayback, string.Empty } };
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

        /// <summary>
        /// Releases used resources
        /// </summary>
        public void Dispose()
        {
            logger.LogMessage("Foreground audio player: disposing.");
            stopProgressUpdateTimer();
            backgroundAudioInitializedEvent.Dispose();
        }

        #endregion
    }
}
