namespace AsotListener.Services.Implementations
{
    using System;
    using System.Threading;
    using Common;
    using Contracts;
    using Models.Enums;
    using Windows.Foundation.Collections;
    using Windows.Foundation.Diagnostics;
    using Windows.Media.Playback;
    using Windows.UI.Xaml.Controls;

    using static Windows.Media.Playback.MediaPlayerState;

    /// <summary>
    /// Contains logic for managing audio playback from foreground application
    /// </summary>
    public sealed class PlaybackManager : IPlaybackManager, IDisposable
    {
        #region Fields

        private const int backgroundAudioWaitingTime = 2000; // 2 sec.

        private readonly ILogger logger;
        private readonly IApplicationSettingsHelper applicationSettingsHelper;
        private readonly AutoResetEvent backgroundAudioInitializedEvent;
        private bool isDisposed = false;

        #endregion

        #region Properties

        private MediaPlayer MediaPlayer => BackgroundMediaPlayer.Current;
        private bool IsBackgroundTaskRunning => applicationSettingsHelper.ReadSettingsValue<bool>(Keys.IsBackgroundTaskRunning);

        #endregion

        #region Ctor

        /// <summary>
        /// Creates new instance of <see cref="PlaybackManager"/>
        /// </summary>
        /// <param name="logger"><see cref="ILogger"/> instance</param>
        /// <param name="applicationSettingsHelper"><see cref="IApplicationSettingsHelper"/> instance</param>
        public PlaybackManager(ILogger logger, IApplicationSettingsHelper applicationSettingsHelper)
        {
            backgroundAudioInitializedEvent = new AutoResetEvent(false);
            this.logger = logger;
            this.applicationSettingsHelper = applicationSettingsHelper;

            // Ensure that Background Audio is initialized by accessing BackgroundMediaPlayer.Current
            var state = MediaPlayer.CurrentState;
            logger.LogMessage($"Foreground playback manager: Current media player state is {state}.", LoggingLevel.Information);

            BackgroundMediaPlayer.MessageReceivedFromBackground += onMessageReceivedFromBackground;
            logger.LogMessage("Foreground playback manager initialized.", LoggingLevel.Information);
        }

        #endregion

        #region Playback Control

        /// <summary>
        /// Switches player to the previous track
        /// </summary>
        public void GoToPreviousTrack()
        {
            logger.LogMessage("Foreground playback manager:  'Previous Track' command fired.");
            BackgroundMediaPlayer.SendMessageToBackground(new ValueSet { { Keys.SkipToPrevious, null } });
        }

        /// <summary>
        /// Starts playback
        /// </summary>
        public void Play()
        {
            logger.LogMessage("Foreground playback manager: 'Play' command fired.");
            WaitForBackgroundAudioTask();
        }

        /// <summary>
        /// Pauses playback
        /// </summary>
        public void Pause()
        {
            logger.LogMessage("Foreground playback manager: 'Pause' command fired.");
            if (IsBackgroundTaskRunning && MediaPlayer.CurrentState == Playing)
            {
                BackgroundMediaPlayer.SendMessageToBackground(new ValueSet { { Keys.PausePlayback, null } });
            }
        }

        /// <summary>
        /// Pauses playback or schedules pause on next resume
        /// </summary>
        public void SchedulePause()
        {
            logger.LogMessage("Foreground playback manager: 'SchedulePause' command fired.");
            if (IsBackgroundTaskRunning)
            {
                BackgroundMediaPlayer.SendMessageToBackground(new ValueSet { { Keys.SchedulePause, null } });
            }
        }

        /// <summary>
        /// Switches player to the next track
        /// </summary>
        public void GoToNextTrack()
        {
            logger.LogMessage("Foreground playback manager: 'Next Track' command fired.");
            BackgroundMediaPlayer.SendMessageToBackground(new ValueSet { { Keys.SkipToNext, null } });
        }

        /// <summary>
        /// Changes current player position based on audio seeker slider value changes
        /// </summary>
        /// <param name="slider">Slider instance</param>
        public void UpdateProgressFromSlider(Slider slider)
        {
            if (MediaPlayer.CurrentState == Playing && slider != null)
            {
                var newPosition = slider.Value < 0 ? 0 : slider.Value;
                var totalSeconds = Math.Round(MediaPlayer.NaturalDuration.TotalSeconds) - 1;
                newPosition = newPosition > totalSeconds ? totalSeconds : newPosition;
                MediaPlayer.Position = TimeSpan.FromSeconds(newPosition);
                logger.LogMessage($"Foreground playback manager: Player position updated to {newPosition} seconds.");
            }
        }

        /// <summary>
        /// Changes current player position based on given amount and interval
        /// </summary>
        /// <param name="howMany">Amount of navigation intervals. Can be negative value.</param>
        /// <param name="interval">Navigation interval</param>
        public void Navigate(int howMany, NavigationInterval interval)
        {
            if (IsBackgroundTaskRunning && (MediaPlayer.CurrentState == Playing || MediaPlayer.CurrentState == Paused))
            {
                BackgroundMediaPlayer.SendMessageToBackground(new ValueSet {
                    { Keys.NavigationAmount, howMany },
                    { Keys.NavigationInterval, (int)interval }
                });
            }
        }

        #endregion

        #region MediaPlayer Event handlers

        private void onMessageReceivedFromBackground(object sender, MediaPlayerDataReceivedEventArgs e)
        {
            if (e.Data.ContainsKey(Keys.IsBackgroundTaskRunning))
            {
                logger.LogMessage("Foreground playback manager: MessageReceivedFromBackground: Background Task started.");
                backgroundAudioInitializedEvent.Set();
            }
        }

        #endregion

        #region Helper Methods

        private void WaitForBackgroundAudioTask()
        {
            logger.LogMessage("Foreground playback manager: waiting for Background Task...");
            if (IsBackgroundTaskRunning)
            {
                backgroundAudioInitializedEvent.Set();
            }

            bool result = backgroundAudioInitializedEvent.WaitOne(backgroundAudioWaitingTime);
            if (result == true)
            {
                logger.LogMessage("Foreground playback manager: Background Task is running. Sending play command.");
                var message = new ValueSet { { Keys.StartPlayback, string.Empty } };
                BackgroundMediaPlayer.SendMessageToBackground(message);
            }
            else
            {
                var message = "Foreground playback manager: Background Audio Task didn't start in expected time";
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
            if (!isDisposed)
            {
                logger.LogMessage("Foreground playback manager: disposing.");
                BackgroundMediaPlayer.MessageReceivedFromBackground -= onMessageReceivedFromBackground;
                backgroundAudioInitializedEvent.Dispose();
                isDisposed = true;
            }
        }

        #endregion
    }
}
