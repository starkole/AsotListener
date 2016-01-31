namespace AsotListener.AudioPlayer
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Common;
    using Models;
    using Models.Enums;
    using Services.Contracts;
    using Windows.Foundation.Collections;
    using Windows.Foundation.Diagnostics;
    using Windows.Media;
    using Windows.Media.Playback;
    using Windows.Storage;
    using Windows.UI.Core;
    using Windows.UI.Popups;
    using static Windows.Media.Playback.MediaPlayerState;

    /// <summary>
    /// Contains logic to manage audio playback
    /// </summary>
    internal sealed class AudioManager : IDisposable
    {
        #region Private Fields

        private bool isPauseScheduled = false;
        private bool isDisposed = false;
        private readonly ILogger logger;
        private readonly IApplicationSettingsHelper applicationSettingsHelper;
        private readonly SystemMediaTransportControls smtc;
        private MediaPlayer mediaPlayer => BackgroundMediaPlayer.Current;
        private Playlist playlist => Playlist.Instance;
        private double correctedTotalDuration => mediaPlayer.NaturalDuration.TotalSeconds - 1;
        private TaskCompletionSource<bool> playlistLoading;

        #endregion

        #region Ctor

        /// <summary>
        /// Creates new instance of <see cref="AudioManager"/>
        /// </summary>
        /// <param name="logger">The logger instance</param>
        /// <param name="applicationSettingsHelper">The application settings helper instance</param>
        /// <param name="smtc">Instance of <see cref="SystemMediaTransportControls"/></param>
        public AudioManager(
            ILogger logger,
            IApplicationSettingsHelper applicationSettingsHelper,
            SystemMediaTransportControls smtc)
        {
            this.logger = logger;
            this.applicationSettingsHelper = applicationSettingsHelper;
            this.smtc = smtc;
            logger.LogMessage("Initializing Background Audio Manager...");
            setupSmtc();
            subscribeToMediaEvents();
            playlistLoading = new TaskCompletionSource<bool>();
            playlistLoading.SetResult(true);
            logger.LogMessage("BackgroundAudio: Background Audio Manager has been initialized.", LoggingLevel.Information);
        }

        private void subscribeToMediaEvents()
        {
            mediaPlayer.MediaEnded -= MediaPlayer_MediaEnded;
            mediaPlayer.MediaEnded += MediaPlayer_MediaEnded;
            mediaPlayer.MediaOpened -= MediaPlayer_MediaOpened;
            mediaPlayer.MediaOpened += MediaPlayer_MediaOpened;
            mediaPlayer.MediaFailed -= MediaPlayer_MediaFailed;
            mediaPlayer.MediaFailed += MediaPlayer_MediaFailed;
            mediaPlayer.CurrentStateChanged -= MediaPlayer_CurrentStateChanged;
            mediaPlayer.CurrentStateChanged += MediaPlayer_CurrentStateChanged;
            smtc.ButtonPressed -= Smtc_ButtonPressed;
            smtc.ButtonPressed += Smtc_ButtonPressed;
            smtc.PropertyChanged -= Smtc_PropertyChanged;
            smtc.PropertyChanged += Smtc_PropertyChanged;
        }

        private void setupSmtc()
        {
            smtc.IsEnabled = true;
            smtc.IsPauseEnabled = true;
            smtc.IsPlayEnabled = true;
            smtc.IsNextEnabled = true;
            smtc.IsPreviousEnabled = true;
        }

        #endregion

        #region MediaPlayer Handlers

        private async void MediaPlayer_CurrentStateChanged(MediaPlayer sender, object args)
        {
            if (sender.CurrentState == Playing)
            {
                logger.LogMessage("BackgroundAudio: Player playing.");
                smtc.PlaybackStatus = MediaPlaybackStatus.Playing;
                if (CanUpdatePlayerPosition())
                {
                    // Start position must be set after payback has already been started
                    sender.Position = playlist.CurrentTrack?.StartPosition ?? TimeSpan.Zero;
                }

                if (isPauseScheduled)
                {
                    isPauseScheduled = false;
                    sender.Pause();
                }

                // Set volume to 100%
                sender.Volume = 1;
            }

            if (sender.CurrentState == Paused)
            {
                logger.LogMessage("BackgroundAudio: Player paused.");
                smtc.PlaybackStatus = MediaPlaybackStatus.Paused;
                if (sender.Position != TimeSpan.Zero)
                {
                    logger.LogMessage("BackgroundAudio: Saving track position.");
                    await SaveCurrentStateAsync();
                }
            }
        }

        private bool CanUpdatePlayerPosition() =>
            playlist.CurrentTrack != null &&
            playlist.CurrentTrack.StartPosition > TimeSpan.Zero &&
            playlist.CurrentTrack.StartPosition < TimeSpan.FromSeconds(correctedTotalDuration);

        private async void MediaPlayer_MediaFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args)
        {
            await showErrorMessageToUserAsync($"BackgroundAudio: Failed to play media file. Error {args.ExtendedErrorCode}. {args.ErrorMessage}");
        }

        private void MediaPlayer_MediaOpened(MediaPlayer sender, object args)
        {
            logger.LogMessage("BackgroundAudio: File opened - start playing.");
            sender.Volume = 0; // Will be set to 1 in MediaPlayer_CurrentStateChanged handler
            sender.Play();
            smtc.PlaybackStatus = MediaPlaybackStatus.Playing;
            smtc.DisplayUpdater.Type = MediaPlaybackType.Music;
            smtc.DisplayUpdater.MusicProperties.Title = playlist.CurrentTrack?.Name ?? string.Empty;
            smtc.DisplayUpdater.MusicProperties.Artist = playlist.CurrentTrack?.Artist ?? string.Empty;
            smtc.DisplayUpdater.MusicProperties.AlbumArtist = playlist.CurrentTrack?.AlbumArtist ?? string.Empty;
            smtc.DisplayUpdater.Update();
        }

        private void MediaPlayer_MediaEnded(MediaPlayer sender, object args)
        {
            logger.LogMessage("BackgroundAudio: Track ended.");
            SkipToNext();
        }

        #endregion

        #region SMTC Handlers

        private void Smtc_PropertyChanged(SystemMediaTransportControls sender, SystemMediaTransportControlsPropertyChangedEventArgs args)
        {
            if (sender.SoundLevel == SoundLevel.Muted && mediaPlayer.CurrentState == Playing)
            {
                logger.LogMessage("BackgroundAudio: Sounds muted - pausing playback.");
                mediaPlayer.Pause();
            }
        }

        private async void Smtc_ButtonPressed(SystemMediaTransportControls sender, SystemMediaTransportControlsButtonPressedEventArgs args)
        {
            switch (args.Button)
            {
                case SystemMediaTransportControlsButton.Play:
                    logger.LogMessage("BackgroundAudio: UVC play button pressed");
                    await StartPlaybackAsync();
                    break;
                case SystemMediaTransportControlsButton.Pause:
                    logger.LogMessage("BackgroundAudio: UVC pause button pressed");
                    mediaPlayer.Pause();
                    break;
                case SystemMediaTransportControlsButton.Next:
                    logger.LogMessage("BackgroundAudio: UVC next button pressed");
                    SkipToNext();
                    break;
                case SystemMediaTransportControlsButton.Previous:
                    logger.LogMessage("BackgroundAudio: UVC previous button pressed");
                    SkipToPrevious();
                    break;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Starts playback from track from given index
        /// </summary>
        /// <param name="index">Track index</param>
        public async void StartTrackAt(int index)
        {
            logger.LogMessage($"BackgroundAudio: Preparing to play the track #{index}.");
            if (mediaPlayer.CurrentState == Playing)
            {
                await SaveCurrentStateAsync();
            }
            playlist.CurrentTrackIndex = index;

            BackgroundMediaPlayer.SendMessageToForeground(new ValueSet { { Keys.CurrentTrack, index } });
            BackgroundMediaPlayer.SendMessageToForeground(new ValueSet { { Keys.CurrentPositionSeconds, playlist.CurrentTrack.StartPosition.TotalSeconds } });
            await StartPlaybackAsync();
        }

        /// <summary>
        /// Advances to the next track
        /// </summary>
        public void SkipToNext()
        {
            logger.LogMessage("BackgroundAudio: Advancing to the next track.");
            smtc.PlaybackStatus = MediaPlaybackStatus.Changing;
            StartTrackAt(playlist.CurrentTrackIndex + 1);
        }

        /// <summary>
        /// Returns to the previous track
        /// </summary>
        public void SkipToPrevious()
        {
            logger.LogMessage("BackgroundAudio: Returning to previous track.");
            smtc.PlaybackStatus = MediaPlaybackStatus.Changing;
            StartTrackAt(playlist.CurrentTrackIndex - 1);
        }

        /// <summary>
        /// Starts playing all tracks from the very first one
        /// </summary>
        public void PlayAllTracks()
        {
            logger.LogMessage("BackgroundAudio: Playing all tracks.");
            StartTrackAt(0);
        }

        /// <summary>
        /// Pauses the playback
        /// </summary>
        public void PausePlayback()
        {
            if (mediaPlayer.CurrentState == Playing)
            {
                logger.LogMessage("BackgroundAudio: Pausing playback manually.");
                mediaPlayer.Pause();
            }
        }

        /// <summary>
        /// Pauses the playback immediately or schedules pause on next playback resume
        /// </summary>
        public void SchedulePause()
        {
            if (mediaPlayer.CurrentState != Playing)
            {
                isPauseScheduled = true;
                logger.LogMessage("BackgroundAudio: Scheduling pause.");
                return;
            }

            logger.LogMessage("BackgroundAudio: No need to schedule pause. Pausing playback now.");
            mediaPlayer.Pause();
            return;
        }

        /// <summary>
        /// Starts playback
        /// </summary>
        public async Task StartPlaybackAsync()
        {
            logger.LogMessage("BackgroundAudio: Trying to start playback.");

            await playlistLoading.Task;
            if (playlist.CurrentTrack == null)
            {
                // If the task was cancelled we would have saved the current track and its position. We will try playback from there
                await applicationSettingsHelper.LoadPlaylist();
                if (playlist.CurrentTrack == null)
                {
                    if (playlist.Any())
                    {
                        playlist.CurrentTrack = playlist[0];
                    }
                    else
                    {
                        logger.LogMessage("BackgroundAudio: Tried to start playback, but no tracks has been found.", LoggingLevel.Warning);
                        return;
                    }
                }
            }

            try
            {
                // Set AutoPlay to false because we set MediaPlayer_MediaOpened event handler to start playback
                mediaPlayer.AutoPlay = false;
                var file = await StorageFile.GetFileFromPathAsync(playlist.CurrentTrack.Uri);
                mediaPlayer.SetFileSource(file);
                logger.LogMessage($"BackgroundAudio: Set file source to {file.Name} ({file.Path}).", LoggingLevel.Information);
            }
            catch (Exception ex)
            {
                await showErrorMessageToUserAsync($"BackgroundAudio: Failed to open media file. {ex.Message}");
            }
        }

        /// <summary>
        /// Saves current player position to disk
        /// </summary>
        /// <returns>Awaitable <see cref="Task"/></returns>
        public async Task SaveCurrentStateAsync()
        {
            logger.LogMessage("BackgroundAudio: Saving current state.");
            if (playlist.CurrentTrack != null)
            {
                playlist.CurrentTrack.StartPosition = mediaPlayer.Position;
            }

            await applicationSettingsHelper.SavePlaylist();
            logger.LogMessage("BackgroundAudio: Current state saved.");
        }

        /// <summary>
        /// Restores player state from disk
        /// </summary>
        /// <returns>Awaitable <see cref="Task"/></returns>
        public async Task LoadPlaylistAsync()
        {
            var oldLoadingTask = playlistLoading.Task;
            playlistLoading = new TaskCompletionSource<bool>();
            await oldLoadingTask;

            logger.LogMessage("BackgroundAudio: Loading playlist from local storage.");
            TimeSpan currentTrackPosition = playlist?.CurrentTrack?.StartPosition ?? TimeSpan.Zero;
            string currentTrackName = playlist?.CurrentTrack?.Name;
            await applicationSettingsHelper.LoadPlaylist();
            if (currentTrackName != null &&
                playlist.CurrentTrack != null &&
                playlist.CurrentTrack.Name == currentTrackName)
            {
                playlist.CurrentTrack.StartPosition = currentTrackPosition;
            }
            playlistLoading.SetResult(true);
            logger.LogMessage("BackgroundAudio: Playlist loaded.");
        }

        /// <summary>
        /// Informs user about critical error
        /// </summary>
        /// <param name="message">Message to display</param>
        /// <returns>Awaitable <see cref="Task"/></returns>
        private async Task showErrorMessageToUserAsync(string message)
        {
            logger.LogMessage(message, LoggingLevel.Critical);
            CoreDispatcher dispatcher = CoreWindow.GetForCurrentThread().Dispatcher;
            await dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                MessageDialog MDialog = new MessageDialog(message, "Error in ASOT Listener audio service");
                await MDialog.ShowAsync();
            });
        }

        private void updatePosition(int seconds)
        {
            if (!(mediaPlayer.CurrentState == Playing || mediaPlayer.CurrentState == Paused) || seconds == 0)
            {
                return;
            }

            double newPositionSeconds = playlist.CurrentTrack.StartPosition.TotalSeconds + seconds;
            if (newPositionSeconds < 0)
            {
                newPositionSeconds = 0;
            }
            else if (newPositionSeconds >= mediaPlayer.NaturalDuration.TotalSeconds)
            {
                newPositionSeconds = correctedTotalDuration;
            }

            var newPosition = TimeSpan.FromSeconds(newPositionSeconds);
            playlist.CurrentTrack.StartPosition = newPosition;
            mediaPlayer.Position = newPosition;
            BackgroundMediaPlayer.SendMessageToForeground(new ValueSet { { Keys.CurrentPositionSeconds, newPositionSeconds } });
        }

        public void Navigate(int howMany, NavigationInterval interval)
        {
            if (!(mediaPlayer.CurrentState == Playing || mediaPlayer.CurrentState == Paused))
            {
                return;
            }

            switch (interval)
            {
                case NavigationInterval.Second:
                    updatePosition(howMany);
                    break;
                case NavigationInterval.Minute:
                    updatePosition(howMany * 60);
                    break;
                case NavigationInterval.Hour:
                    updatePosition(howMany * 60 * 60);
                    break;
                case NavigationInterval.Track:
                case NavigationInterval.Episode:
                    StartTrackAt(playlist.CurrentTrackIndex + howMany);
                    break;
            }
        }

        #endregion

        #region IDisposable Support

        /// <summary>
        /// Unsubscribes event handlers
        /// </summary>
        public void Dispose()
        {
            if (!isDisposed)
            {
                mediaPlayer.MediaEnded -= MediaPlayer_MediaEnded;
                mediaPlayer.MediaOpened -= MediaPlayer_MediaOpened;
                mediaPlayer.MediaFailed -= MediaPlayer_MediaFailed;
                mediaPlayer.CurrentStateChanged -= MediaPlayer_CurrentStateChanged;
                smtc.ButtonPressed -= Smtc_ButtonPressed;
                smtc.PropertyChanged -= Smtc_PropertyChanged;
                isDisposed = true;
            }
        }

        #endregion
    }
}
