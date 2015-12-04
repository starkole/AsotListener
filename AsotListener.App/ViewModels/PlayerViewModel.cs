namespace AsotListener.App.ViewModels
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using System.Windows.Input;
    using Models;
    using Services;
    using Windows.Foundation;
    using Windows.Foundation.Collections;
    using Windows.Media.Playback;
    using Windows.UI.Xaml.Controls;

    public class PlayerViewModel : BaseModel, IDisposable
    {
        #region Fields

        private MediaPlayer mediaPlayer;
        private bool isPreviousButtonEnabled;
        private bool isNextButtonEnabled;
        private bool isPlayButtonEnabled;
        private IconElement pauseIcon = new SymbolIcon(Symbol.Pause);
        private IconElement playIcon = new SymbolIcon(Symbol.Play);
        private IconElement playButtonIcon;
        private bool isMyBackgroundTaskRunning = false;
        private IApplicationSettingsHelper applicationSettingsHelper;
        private AutoResetEvent ServerInitialized;

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

        public IconElement PlayButtonIcon
        {
            get { return playButtonIcon; }
            set { SetField(ref playButtonIcon, value, nameof(PlayButtonIcon)); }
        }

        public ICommand PreviousTrackCommand { get; }
        public ICommand NextTrackCommand { get; }
        public ICommand PlayPauseCommand { get; }

        public IPlayList Playlist { get; }

        #endregion

        #region Ctor

        public PlayerViewModel()
        {
            mediaPlayer = BackgroundMediaPlayer.Current;
            Playlist = Services.Playlist.Instance;
            applicationSettingsHelper = ApplicationSettingsHelper.Instance;

            PlayButtonIcon = playIcon;

            // Using explicit casts here because of http://stackoverflow.com/questions/2057146/compiler-ambiguous-invocation-error-anonymous-method-and-method-group-with-fun
            PreviousTrackCommand = new RelayCommand((Action)onPreviousTrackAction);
            NextTrackCommand = new RelayCommand((Action)onNextButtonAction);
            PlayPauseCommand = new RelayCommand((Action)onPlayPauseAction);

            updateBackgroundTaskRunningStatus();
            ServerInitialized = new AutoResetEvent(false);

            //Adding App suspension handlers here so that we can unsubscribe handlers 
            //that access to BackgroundMediaPlayer events
            App.Current.Suspending += ForegroundApp_Suspending;
            App.Current.Resuming += ForegroundApp_Resuming;
        }

        #endregion

        #region Foreground App Lifecycle Handlers
        
        /// <summary>
        /// Sends message to background informing app has resumed
        /// Subscribe to MediaPlayer events
        /// </summary>
        void ForegroundApp_Resuming(object sender, object e)
        {
            applicationSettingsHelper.SaveSettingsValue(Constants.AppState, Constants.ForegroundAppActive);

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

        }

        /// <summary>
        /// Send message to Background process that app is to be suspended
        /// Stop clock and slider when suspending
        /// Unsubscribe handlers for MediaPlayer events
        /// </summary>
        void ForegroundApp_Suspending(object sender, Windows.ApplicationModel.SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            ValueSet message = new ValueSet() { { Constants.AppSuspended, DateTime.Now.ToString() } };
            BackgroundMediaPlayer.SendMessageToBackground(message);
            RemoveMediaPlayerEventHandlers();
            applicationSettingsHelper.SaveSettingsValue(Constants.AppState, Constants.ForegroundAppSuspended);
            deferral.Complete();
        }
        
        #endregion

        #region Button Commands

        /// <summary>
        /// Sends message to the background task to skip to the previous track.
        /// </summary>
        private void onPreviousTrackAction()
        {
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
            Debug.WriteLine("Play button pressed from App");

            // Play button will be enabled when media player will be ready
            IsPlayButtonEnabled = false;
            updateBackgroundTaskRunningStatus();
            if (isMyBackgroundTaskRunning)
            {
                if (MediaPlayerState.Playing == BackgroundMediaPlayer.Current.CurrentState)
                {
                    BackgroundMediaPlayer.Current.Pause();
                }
                else if (MediaPlayerState.Paused == BackgroundMediaPlayer.Current.CurrentState)
                {
                    BackgroundMediaPlayer.Current.Play();
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
        void MediaPlayer_CurrentStateChanged(MediaPlayer sender, object args)
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
                    //Wait for Background Task to be initialized before starting playback
                    Debug.WriteLine("Background Task started");
                    ServerInitialized.Set();
                    return;
                }
            }
        }

        #endregion

        #region Helper Methods
        private void updateBackgroundTaskRunningStatus()
        {
            object value = applicationSettingsHelper.ReadSettingsValue(Constants.BackgroundTaskState);
            if (value == null)
            {
                isMyBackgroundTaskRunning = false;
            }

            isMyBackgroundTaskRunning = ((string)value).Equals(Constants.BackgroundTaskRunning);
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
            BackgroundMediaPlayer.Current.CurrentStateChanged += this.MediaPlayer_CurrentStateChanged;
            BackgroundMediaPlayer.MessageReceivedFromBackground += this.BackgroundMediaPlayer_MessageReceivedFromBackground;
        }

        /// <summary>
        /// Initialize Background Media Player Handlers and starts playback
        /// </summary>
        private void StartBackgroundAudioTask()
        {
            AddMediaPlayerEventHandlers();
            bool result = ServerInitialized.WaitOne(2000);
            if (result == true)
            {
                var message = new ValueSet() { { Constants.StartPlayback, "0" } };
                BackgroundMediaPlayer.SendMessageToBackground(message);
            }
            else
            {
                throw new Exception("Background Audio Task didn't start in expected time");
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
                    this.ServerInitialized.Dispose();
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
