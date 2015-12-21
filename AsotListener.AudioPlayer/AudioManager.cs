namespace AsotListener.AudioPlayer
{
    using System;
    using System.Linq;
    using Models;
    using Windows.Media;
    using Windows.Media.Playback;
    using Windows.Storage;
    using Services.Contracts;
    using Windows.Foundation.Diagnostics;
    using Windows.UI.Core;
    using Windows.UI.Popups;
    using System.Threading.Tasks;

    internal class AudioManager : IDisposable
    {
        #region Private Fields

        private bool isDisposed = false;
        private readonly ILogger logger;
        private IPlayList playlist;
        private MediaPlayer MediaPlayer => BackgroundMediaPlayer.Current;
        private SystemMediaTransportControls smtc;

        #endregion

        #region Ctor

        public AudioManager(
            ILogger logger,
            IPlayList playlist,
            SystemMediaTransportControls smtc)
        {
            this.logger = logger;
            logger.LogMessage("Initializing Background Audio Manager...");
            this.playlist = playlist;

            MediaPlayer.MediaEnded += MediaPlayer_MediaEnded;
            MediaPlayer.MediaOpened += MediaPlayer_MediaOpened;
            MediaPlayer.MediaFailed += MediaPlayer_MediaFailed;
            MediaPlayer.CurrentStateChanged += MediaPlayer_CurrentStateChanged;

            this.smtc = smtc;
            smtc.ButtonPressed += Smtc_ButtonPressed;
            smtc.PropertyChanged += Smtc_PropertyChanged;
            smtc.IsEnabled = true;
            smtc.IsPauseEnabled = true;
            smtc.IsPlayEnabled = true;
            smtc.IsNextEnabled = true;
            smtc.IsPreviousEnabled = true;

            logger.LogMessage("BackgroundAudio: Background Audio Manager has been initialized.", LoggingLevel.Information);
        }

        #endregion

        #region MediaPlayer Handlers

        private async void MediaPlayer_CurrentStateChanged(MediaPlayer sender, object args)
        {
            if (sender.CurrentState == MediaPlayerState.Playing)
            {
                logger.LogMessage("BackgroundAudio: Player playing.");
                smtc.PlaybackStatus = MediaPlaybackStatus.Playing;
                if (CanUpdatePlayerPosition())
                {
                    // Start position must be set after payback has already been started
                    sender.Position = playlist.CurrentTrack.StartPosition;
                }

                // Set volume to 100%
                sender.Volume = 1;
            }

            if (sender.CurrentState == MediaPlayerState.Paused)
            {
                logger.LogMessage("BackgroundAudio: Player paused.");
                smtc.PlaybackStatus = MediaPlaybackStatus.Paused;
                if (sender.Position != TimeSpan.Zero)
                {
                    logger.LogMessage("BackgroundAudio: Saving track position.");
                    await SaveCurrentState();
                }
            }
        }

        private bool CanUpdatePlayerPosition() =>
            playlist.CurrentTrack != null &&
            playlist.CurrentTrack.StartPosition > TimeSpan.Zero &&
            playlist.CurrentTrack.StartPosition < TimeSpan.FromSeconds(MediaPlayer.NaturalDuration.TotalSeconds - 1);

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
            if (sender.SoundLevel == SoundLevel.Muted)
            {
                logger.LogMessage("BackgroundAudio: Sounds muted - pausing playback.");
                MediaPlayer.Pause();
            }
        }

        private void Smtc_ButtonPressed(SystemMediaTransportControls sender, SystemMediaTransportControlsButtonPressedEventArgs args)
        {
            switch (args.Button)
            {
                case SystemMediaTransportControlsButton.Play:
                    logger.LogMessage("BackgroundAudio: UVC play button pressed");
                    StartPlayback();
                    break;
                case SystemMediaTransportControlsButton.Pause:
                    logger.LogMessage("BackgroundAudio: UVC pause button pressed");
                    MediaPlayer.Pause();
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

        public void StartTrackAt(int rawIndex)
        {
            logger.LogMessage($"BackgroundAudio: Preparing to play the track #{rawIndex}.");
            if (!playlist.TrackList.Any())
            {
                return;
            }

            int tracksCount = playlist.TrackList.Count;
            int index = rawIndex < 0 ?
                (tracksCount - rawIndex) % tracksCount :
                rawIndex % tracksCount;
            playlist.CurrentTrack = playlist.TrackList[index];
            StartPlayback();
        }

        public void StartTrackAt(AudioTrack audioTrack)
        {
            logger.LogMessage($"BackgroundAudio: Preparing to play the track {audioTrack.Name}.");
            if (!playlist.TrackList.Contains(audioTrack))
            {
                playlist.TrackList.Add(audioTrack);
            }

            playlist.CurrentTrack = audioTrack;
            StartPlayback();
        }

        public void SkipToNext()
        {
            logger.LogMessage("BackgroundAudio: Advancing to the next track.");
            smtc.PlaybackStatus = MediaPlaybackStatus.Changing;
            StartTrackAt(playlist.CurrentTrackIndex + 1);
        }

        public void SkipToPrevious()
        {
            logger.LogMessage("BackgroundAudio: Returning to previous track.");
            smtc.PlaybackStatus = MediaPlaybackStatus.Changing;
            StartTrackAt(playlist.CurrentTrackIndex - 1);
        }

        public void PlayAllTracks()
        {
            logger.LogMessage("BackgroundAudio: Playing all tracks.");
            StartTrackAt(0);
        }

        public void PausePlayback()
        {
            if (MediaPlayer.CurrentState == MediaPlayerState.Playing)
            {
                logger.LogMessage("BackgroundAudio: Pausing playback manually.");
                MediaPlayer.Pause();
            }
        }

        public async void StartPlayback()
        {
            logger.LogMessage("BackgroundAudio: Trying to start playback.");
            if (playlist.CurrentTrack == null)
            {
                //If the task was cancelled we would have saved the current track and its position. We will try playback from there
                await playlist.LoadPlaylistFromLocalStorage();
                if (playlist.CurrentTrack == null)
                {
                    if (playlist.TrackList.Any())
                    {
                        playlist.CurrentTrack = playlist.TrackList[0];
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
                MediaPlayer.AutoPlay = false;
                var file = await StorageFile.GetFileFromPathAsync(playlist.CurrentTrack.Uri);
                MediaPlayer.SetFileSource(file);
                logger.LogMessage($"BackgroundAudio: Set file source to {file.Name} ({file.Path}).", LoggingLevel.Information);
            }
            catch (Exception ex)
            {
                await showErrorMessageToUserAsync($"BackgroundAudio: Failed to open media file. {ex.Message}");
            }
        }

        public async Task SaveCurrentState()
        {
            logger.LogMessage("BackgroundAudio: Saving current state.");
            if (playlist.CurrentTrack != null)
            {
                playlist.CurrentTrack.StartPosition = MediaPlayer.Position;
            }

            await playlist.SavePlaylistToLocalStorage();
            logger.LogMessage("BackgroundAudio: Current state saved.");
        }

        public async Task LoadState()
        {
            logger.LogMessage("BackgroundAudio: Loading playlist from local storage.");
            await playlist.LoadPlaylistFromLocalStorage();
            logger.LogMessage("BackgroundAudio: Current state loaded.");
        }

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


        #endregion

        #region IDisposable Support

        protected virtual void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                if (disposing)
                {
                    MediaPlayer.Pause();

                    MediaPlayer.MediaEnded -= MediaPlayer_MediaEnded;
                    MediaPlayer.MediaOpened -= MediaPlayer_MediaOpened;
                    MediaPlayer.MediaFailed -= MediaPlayer_MediaFailed;
                    MediaPlayer.CurrentStateChanged -= MediaPlayer_CurrentStateChanged;

                    smtc.ButtonPressed -= Smtc_ButtonPressed;
                    smtc.PropertyChanged -= Smtc_PropertyChanged;
                }

                isDisposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        #endregion
    }
}
