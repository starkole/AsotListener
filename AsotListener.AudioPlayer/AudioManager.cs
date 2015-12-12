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

    internal class AudioManager : IDisposable
    {
        #region Private Fields

        private readonly ILogger logger;
        private IPlayList playlist;
        private MediaPlayer mediaPlayer;
        private SystemMediaTransportControls smtc;
        private Action backgroundTaskWaitAction;

        #endregion

        #region Ctor

        public AudioManager(
            ILogger logger,
            IPlayList playlist,
            MediaPlayer mediaPlayer,
            SystemMediaTransportControls smtc,
            Action backgroundTaskWaitAction)
        {
            this.logger = logger;
            logger.LogMessage("Initializing Background Audio Manager...");

            this.backgroundTaskWaitAction = backgroundTaskWaitAction;
            this.playlist = playlist;
            this.mediaPlayer = mediaPlayer;
            mediaPlayer.MediaEnded += MediaPlayer_MediaEnded;
            mediaPlayer.MediaOpened += MediaPlayer_MediaOpened;
            mediaPlayer.MediaFailed += MediaPlayer_MediaFailed;
            mediaPlayer.CurrentStateChanged += MediaPlayer_CurrentStateChanged;

            this.smtc = smtc;
            smtc.ButtonPressed += Smtc_ButtonPressed;
            smtc.PropertyChanged += Smtc_PropertyChanged;
            smtc.IsEnabled = true;
            smtc.IsPauseEnabled = true;
            smtc.IsPlayEnabled = true;
            smtc.IsNextEnabled = true;
            smtc.IsPreviousEnabled = true;

            logger.LogMessage("Background Audio Manager has been initialized.");
        }

        #endregion

        #region MediaPlayer Handlers

        private void MediaPlayer_CurrentStateChanged(MediaPlayer sender, object args)
        {
            if (sender.CurrentState == MediaPlayerState.Playing)
            {
                logger.LogMessage("BackgroundAudio: Player playing.");
                smtc.PlaybackStatus = MediaPlaybackStatus.Playing;
                if (playlist.CurrentTrack.StartPosition != TimeSpan.Zero)
                {
                    // Start position must be set after payback has already been started
                    sender.Position = playlist.CurrentTrack.StartPosition;                    
                }

                sender.Volume = 1;
            }

            if (sender.CurrentState == MediaPlayerState.Paused)
            {
                logger.LogMessage("BackgroundAudio: Player paused.");
                smtc.PlaybackStatus = MediaPlaybackStatus.Paused;
                if (sender.Position != TimeSpan.Zero)
                {
                    logger.LogMessage("BackgroundAudio: Saving track position.");
                    playlist.CurrentTrack.StartPosition = sender.Position;
                    SaveCurrentState();
                }
            }
        }

        private async void MediaPlayer_MediaFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args)
        {
            string message = $"BackgroundAudio: Failed to open media file. Error {args.ExtendedErrorCode}. {args.ErrorMessage}";
            logger.LogMessage(message);
            CoreDispatcher dispatcher = CoreWindow.GetForCurrentThread().Dispatcher;
            await dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                MessageDialog MDialog = new MessageDialog(message, "Error in ASOT Listener audio service");
                await MDialog.ShowAsync();
            });
        }

        private void MediaPlayer_MediaOpened(MediaPlayer sender, object args)
        {
            logger.LogMessage("BackgroundAudio: File opened - start playing.");
            sender.Volume = 0; // Will be set to 1 in MediaPlayer_CurrentStateChanged handler
            sender.Play();
            smtc.PlaybackStatus = MediaPlaybackStatus.Playing;
            smtc.DisplayUpdater.Type = MediaPlaybackType.Music;
            smtc.DisplayUpdater.MusicProperties.Title = playlist.CurrentTrack.Name;
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
                mediaPlayer.Pause();
            }
        }

        private void Smtc_ButtonPressed(SystemMediaTransportControls sender, SystemMediaTransportControlsButtonPressedEventArgs args)
        {
            switch (args.Button)
            {
                case SystemMediaTransportControlsButton.Play:
                    logger.LogMessage("UVC play button pressed");
                    backgroundTaskWaitAction();
                    StartPlayback();
                    break;
                case SystemMediaTransportControlsButton.Pause:
                    logger.LogMessage("UVC pause button pressed");
                    mediaPlayer.Pause();
                    break;
                case SystemMediaTransportControlsButton.Next:
                    logger.LogMessage("UVC next button pressed");
                    SkipToNext();
                    break;
                case SystemMediaTransportControlsButton.Previous:
                    logger.LogMessage("UVC previous button pressed");
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
            logger.LogMessage("BackgroundAudio: Going to previous track.");
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
            if (mediaPlayer.CurrentState == MediaPlayerState.Playing)
            {
                logger.LogMessage("BackgroundAudio: Pausing playback manually.");
                mediaPlayer.Pause();
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

            // Set AutoPlay to false because we set MediaPlayer_MediaOpened event handler to start playback
            mediaPlayer.AutoPlay = false;
            var file = await StorageFile.GetFileFromPathAsync(playlist.CurrentTrack.Uri);
            mediaPlayer.SetFileSource(file);
            logger.LogMessage($"BackgroundAudio: Set file source to {file.Name} ({file.Path}).");
        }

        public void SaveCurrentState()
        {
            logger.LogMessage("BackgroundAudio: Saving current state.");
            playlist.CurrentTrack.StartPosition = mediaPlayer.Position;
            playlist.SavePlaylistToLocalStorage();
        }

        public void LoadState()
        {
            logger.LogMessage("BackgroundAudio: Loading playlist from local storage.");
            playlist.LoadPlaylistFromLocalStorage();
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
                    mediaPlayer.Pause();
                    SaveCurrentState();

                    mediaPlayer.MediaEnded -= MediaPlayer_MediaEnded;
                    mediaPlayer.MediaOpened -= MediaPlayer_MediaOpened;
                    mediaPlayer.MediaFailed -= MediaPlayer_MediaFailed;
                    mediaPlayer.CurrentStateChanged -= MediaPlayer_CurrentStateChanged;

                    smtc.ButtonPressed -= Smtc_ButtonPressed;
                    smtc.PropertyChanged -= Smtc_PropertyChanged;
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
