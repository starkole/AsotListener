namespace AsotListener.AudioPlayer
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using Models;
    using Windows.Media;
    using Windows.Media.Playback;
    using Services;

    internal class AudioManager : IDisposable
    {
        #region Private Fields

        private IPlayList playlist;
        private MediaPlayer mediaPlayer;
        private SystemMediaTransportControls smtc;
        private Action backgroundTaskWaitAction;

        #endregion

        #region Ctor

        public AudioManager(
            IPlayList playlist, 
            MediaPlayer mediaPlayer, 
            SystemMediaTransportControls smtc, 
            Action backgroundTaskWaitAction)
        {
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
        }

        #endregion

        #region MediaPlayer Handlers

        private void MediaPlayer_CurrentStateChanged(MediaPlayer sender, object args)
        {
            if (sender.CurrentState == MediaPlayerState.Playing)
            {
                smtc.PlaybackStatus = MediaPlaybackStatus.Playing;
                if (playlist.CurrentTrack.StartPosition != TimeSpan.FromSeconds(0))
                {
                    // Start position must be set after payback has already been started
                    sender.Position = playlist.CurrentTrack.StartPosition;
                    playlist.CurrentTrack.StartPosition = TimeSpan.FromSeconds(0);
                }
            }

            if (sender.CurrentState == MediaPlayerState.Paused)
            {
                smtc.PlaybackStatus = MediaPlaybackStatus.Paused;
            }
        }

        private void MediaPlayer_MediaFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args)
        {
            // TODO: Show error message to user and log error.
        }

        private void MediaPlayer_MediaOpened(MediaPlayer sender, object args)
        {
            sender.Play();
            updateUVCOnNewTrack();
        }

        private void MediaPlayer_MediaEnded(MediaPlayer sender, object args)
        {
            SkipToNext();
        }

        #endregion

        #region SMTC Handlers

        private void Smtc_PropertyChanged(SystemMediaTransportControls sender, SystemMediaTransportControlsPropertyChangedEventArgs args)
        {
            if (sender.SoundLevel == SoundLevel.Muted)
            {
                mediaPlayer.Pause();
            }
        }

        private void Smtc_ButtonPressed(SystemMediaTransportControls sender, SystemMediaTransportControlsButtonPressedEventArgs args)
        {
            switch (args.Button)
            {
                case SystemMediaTransportControlsButton.Play:
                    Debug.WriteLine("UVC play button pressed");
                    backgroundTaskWaitAction();
                    playCurrentTrack();
                    break;
                case SystemMediaTransportControlsButton.Pause:
                    Debug.WriteLine("UVC pause button pressed");
                    mediaPlayer.Pause();
                    break;
                case SystemMediaTransportControlsButton.Next:
                    Debug.WriteLine("UVC next button pressed");
                    SkipToNext();
                    break;
                case SystemMediaTransportControlsButton.Previous:
                    Debug.WriteLine("UVC previous button pressed");
                    SkipToPrevious();
                    break;
            }
        }

        #endregion

        #region Public Methods

        public void StartTrackAt(int rawIndex)
        {
            if (!playlist.TrackList.Any())
            {
                return;
            }

            int tracksCount = playlist.TrackList.Count;
            int index = rawIndex < 0 ?
                (tracksCount - rawIndex) % tracksCount :
                rawIndex % tracksCount;
            playlist.CurrentTrack = playlist.TrackList[index];
            playCurrentTrack();
        }

        public void StartTrackAt(AudioTrack audioTrack)
        {
            if (!playlist.TrackList.Contains(audioTrack))
            {
                playlist.TrackList.Add(audioTrack);
            }

            playlist.CurrentTrack = audioTrack;
            playCurrentTrack();
        }

        public void SkipToNext()
        {
            smtc.PlaybackStatus = MediaPlaybackStatus.Changing;
            StartTrackAt(playlist.CurrentTrackIndex + 1);
        }

        public void SkipToPrevious()
        {
            smtc.PlaybackStatus = MediaPlaybackStatus.Changing;
            StartTrackAt(playlist.CurrentTrackIndex - 1);
        }

        public void PlayAllTracks()
        {
            StartTrackAt(0);
        }

        #endregion

        #region Private Methods

        private void updateUVCOnNewTrack()
        {
            smtc.PlaybackStatus = MediaPlaybackStatus.Playing;
            smtc.DisplayUpdater.Type = MediaPlaybackType.Music;
            smtc.DisplayUpdater.MusicProperties.Title = playlist.CurrentTrack.Name;
            smtc.DisplayUpdater.Update();
        }

        private void playCurrentTrack()
        {
            if (playlist.CurrentTrack == null)
            {
                //If the task was cancelled we would have saved the current track and its position. We will try playback from there
                playlist.LoadPlaylistFromLocalStorage();
                if (playlist.CurrentTrack == null)
                {
                    if (playlist.TrackList.Any())
                    {
                        playlist.CurrentTrack = playlist.TrackList[0];
                    }
                    else
                    {
                        return;
                    }
                }
            }

            // Set AutoPlay to false because we set MediaPlayer_MediaOpened event handler to start playback
            mediaPlayer.AutoPlay = false;
            mediaPlayer.SetUriSource(new Uri(playlist.CurrentTrack.Uri));
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
