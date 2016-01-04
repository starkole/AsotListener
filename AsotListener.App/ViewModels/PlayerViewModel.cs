namespace AsotListener.App.ViewModels
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows.Input;
    using Common;
    using Models;
    using Models.Enums;
    using Services.Contracts;
    using Windows.ApplicationModel.Core;
    using Windows.Foundation.Collections;
    using Windows.Foundation.Diagnostics;
    using Windows.Media.Playback;
    using Windows.UI.Core;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;

    using static Windows.Media.Playback.MediaPlayerState;

    /// <summary>
    /// View model of audio player
    /// </summary>
    public sealed class PlayerViewModel : BaseModel, IDisposable, IAsyncInitialization
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
        private const int backgroundAudioWaitingTime = 2000; // 2 sec.
        private readonly ILogger logger;
        private readonly INavigationService navigationService;
        private readonly IPlaybackManager playbackManager;

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

        public Task Initialization { get; }

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
            INavigationService navigationService,
            IPlaybackManager playbackManager)
        {
            this.logger = logger;
            this.navigationService = navigationService;
            this.applicationSettingsHelper = applicationSettingsHelper;
            this.playbackManager = playbackManager;
            pauseIcon = new SymbolIcon(Symbol.Pause);
            playIcon = new SymbolIcon(Symbol.Play);
            PlayButtonIcon = playIcon;
            progressUpdateTimer = new DispatcherTimer();

            // Using explicit casts here because of http://stackoverflow.com/questions/2057146/compiler-ambiguous-invocation-error-anonymous-method-and-method-group-with-fun
            PreviousTrackCommand = new RelayCommand((Action)onPreviousTrackAction);
            NextTrackCommand = new RelayCommand((Action)onNextTrackAction);
            PlayPauseCommand = new RelayCommand((Action)onPlayPauseAction);

            Application.Current.Suspending += onAppSuspending;
            Application.Current.Resuming += onAppResuming;

            Initialization = initializeAsync();
            logger.LogMessage("Foreground audio player initialized.", LoggingLevel.Information);
        }

        #endregion

        #region Foreground App Lifecycle Handlers

        /// <summary>
        /// Sends message to background informing app has resumed
        /// Subscribe to MediaPlayer events
        /// </summary>
        private async Task initializeAsync()
        {
            logger.LogMessage("Foreground audio player updating current state...");

            await applicationSettingsHelper.LoadPlaylist();
            if (Playlist.Instance.CurrentTrack == null)
            {
                logger.LogMessage("Foreground audio player no current track selected in playlist.");
                return;
            }

            CurrentTrackName = Playlist.Instance.CurrentTrack.Name; 
            // TODO: How do I know if it is my track playing now?
            PlayButtonIcon = MediaPlayer.CurrentState == Playing ? pauseIcon : playIcon;
            IsNextButtonEnabled = true;
            IsPlayButtonEnabled = true;
            IsPreviousButtonEnabled = true;
            setupAudioProgress();
            navigationService.Navigate(NavigationParameter.OpenPlayer);
            if (IsBackgroundTaskRunning)
            {
                addMediaPlayerEventHandlers();
                if (MediaPlayer.CurrentState == Playing ||
                    MediaPlayer.CurrentState == Paused)
                {
                    startProgressUpdateTimer();
                }
            }

            logger.LogMessage("Foreground audio player current state updated.");
        }

        private void onAppSuspending(object sender, Windows.ApplicationModel.SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            stopProgressUpdateTimer();
            removeMediaPlayerEventHandlers();
            logger.LogMessage("Foreground audio player suspended.", LoggingLevel.Information);
            deferral.Complete();
        }

        private async void onAppResuming(object sender, object e)
        {
            await initializeAsync();
        }

        #endregion

        #region Button Commands

        private void onPreviousTrackAction()
        {
            playbackManager.GoToPreviousTrack();

            // Prevent the user from repeatedly pressing the button and causing 
            // a back-long of button presses to be handled. This button is re-enabled 
            // in the TrackReady Play state handler.
            IsPreviousButtonEnabled = false;
        }

        private void onPlayPauseAction()
        {
            // Play button will be enabled when media player will be ready
            IsPlayButtonEnabled = false;
            if (MediaPlayer.CurrentState == Playing)
            {
                playbackManager.Pause();
            } else
            {
                playbackManager.Play();
            }
        }

        private void onNextTrackAction()
        {
            playbackManager.GoToNextTrack();

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


            if (sender.CurrentState == Opening)
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    stopProgressUpdateTimer();
                    setupAudioProgress();
                });


            if (sender.CurrentState == Playing)
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    IsPlayButtonEnabled = true;
                    IsNextButtonEnabled = true;
                    IsPreviousButtonEnabled = true;
                    IsAudioSeekerEnabled = true;
                    PlayButtonIcon = pauseIcon;
                    setupAudioProgress();
                    startProgressUpdateTimer();
                });


            if (sender.CurrentState == Paused)
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    IsPlayButtonEnabled = true;
                    PlayButtonIcon = playIcon;
                    stopProgressUpdateTimer();
                });
        }

        private void onMessageReceivedFromBackground(object sender, MediaPlayerDataReceivedEventArgs e)
        {
            foreach (var key in e.Data.Keys)
            {
                switch (key)
                {
                    case Keys.CurrentTrack:
                        int index = (int)e.Data[Keys.CurrentTrack];
                        logger.LogMessage($"Foreground audio player MessageReceivedFromBackground: Current track changed to {index}.");
                        Playlist.Instance.CurrentTrackIndex = index;
                        break;
                }
            }
        }

        #endregion

        #region Progress Tracking

        /// <summary>
        /// Changes current player position based on audio seeker slider value changes
        /// </summary>
        /// <param name="slider">Slider instance</param>
        public void UpdateProgressFromSlider(Slider slider) => playbackManager.UpdateProgressFromSlider(slider);

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
            if (MediaPlayer.NaturalDuration <= TimeSpan.Zero)
            {
                return;
            }

            AudioSeekerStepFrequency = getAudioSeekerStepFrequency(MediaPlayer.NaturalDuration);
            CurrentTrackLeftToplay = (MediaPlayer.NaturalDuration - MediaPlayer.Position).ToUserFriendlyString();
            CurrentTrackPlayedTime = MediaPlayer.Position.ToUserFriendlyString();
            AudioSeekerMaximum = Math.Round(MediaPlayer.NaturalDuration.TotalSeconds) + 1;
            AudioSeekerValue = Math.Round(MediaPlayer.Position.TotalSeconds);
            CurrentTrackName = Playlist.Instance.CurrentTrack.Name;
        }

        private void startProgressUpdateTimer()
        {
            if (!progressUpdateTimer.IsEnabled)
            {
                IsAudioSeekerEnabled = true;
                progressUpdateTimer.Tick -= onTimerTick; // Ensure we subscribed only one handler
                progressUpdateTimer.Tick += onTimerTick;
                progressUpdateTimer.Start();
                logger.LogMessage($"Foreground audio player: Progress update timer started.", LoggingLevel.Information);
            }
        }

        private void stopProgressUpdateTimer()
        {
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
                CurrentTrackLeftToplay = (MediaPlayer.NaturalDuration - MediaPlayer.Position).ToUserFriendlyString();
                CurrentTrackPlayedTime = MediaPlayer.Position.ToUserFriendlyString();
                AudioSeekerValue = Math.Round(MediaPlayer.Position.TotalSeconds);
            }
        }

        #endregion

        #region Helper Methods

        private void removeMediaPlayerEventHandlers()
        {
            MediaPlayer.CurrentStateChanged -= onMediaPlayerCurrentStateChanged;
            BackgroundMediaPlayer.MessageReceivedFromBackground -= onMessageReceivedFromBackground;
        }

        private void addMediaPlayerEventHandlers()
        {
            // Ensure we subscribe handlers only once
            MediaPlayer.CurrentStateChanged -= onMediaPlayerCurrentStateChanged;
            BackgroundMediaPlayer.MessageReceivedFromBackground -= onMessageReceivedFromBackground;

            MediaPlayer.CurrentStateChanged += onMediaPlayerCurrentStateChanged;
            BackgroundMediaPlayer.MessageReceivedFromBackground += onMessageReceivedFromBackground;
        }        

        #endregion

        #region IDisposable

        /// <summary>
        /// Releases used resources
        /// </summary>
        public void Dispose()
        {
            logger.LogMessage("Foreground audio player: disposing.");
            removeMediaPlayerEventHandlers();
            stopProgressUpdateTimer();
        }

        #endregion
    }
}
