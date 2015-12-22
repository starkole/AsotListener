namespace AsotListener.AudioPlayer
{
    using System;
    using Windows.ApplicationModel.Background;
    using Windows.Media;
    using Windows.Media.Playback;
    using Windows.Foundation.Collections;
    using Services.Contracts;
    using Windows.Foundation.Diagnostics;
    using Ioc;
    using Common;

    /// <summary>
    /// Impalements IBackgroundTask to provide an entry point for app code to be run in background. 
    /// Also takes care of handling UVC and communication channel with foreground
    /// </summary>
    public sealed class AudioPlayerTask : IBackgroundTask, IDisposable
    {
        #region Private fields

        private bool isDisposed = false;
        private AudioManager audioManager;
        private BackgroundTaskDeferral deferral;
        private IApplicationSettingsHelper applicationSettingsHelper;
        private ILogger logger;

        #endregion

        #region IBackgroundTask and IBackgroundTaskInstance Interface Members and handlers

        /// <summary>
        /// The Run method is the entry point of a background task. 
        /// </summary>
        /// <param name="taskInstance"></param>
        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            deferral = taskInstance.GetDeferral();
            taskInstance.Canceled += OnCanceled;
            taskInstance.Task.Completed += Taskcompleted;

            IContainer container = Container.Instance;
            Services.IoC.Register();

            logger = container.Resolve<ILogger>();
            logger.LogMessage($"BackgroundAudioTask {taskInstance.Task.Name} starting...");

            try {

                // Ensure that Background Audio is initialized by accessing BackgroundMediaPlayer.Current
                var state = BackgroundMediaPlayer.Current.CurrentState;
                logger.LogMessage($"BackgroundAudioTask BackgroundMediaPlayer state is {state}.", LoggingLevel.Information);

                applicationSettingsHelper = container.Resolve<IApplicationSettingsHelper>();
                IPlayList playlist = container.Resolve<IPlayList>();
                await playlist.LoadPlaylistFromLocalStorage();

                audioManager = new AudioManager(
                    logger,
                    playlist,
                    SystemMediaTransportControls.GetForCurrentView());

                BackgroundMediaPlayer.MessageReceivedFromForeground += BackgroundMediaPlayer_MessageReceivedFromForeground;

                applicationSettingsHelper.SaveSettingsValue(Constants.IsBackgroundTaskRunning, true);

                ValueSet message = new ValueSet { { Constants.IsBackgroundTaskRunning, true } };
                BackgroundMediaPlayer.SendMessageToForeground(message);
                logger.LogMessage($"BackgroundAudioTask initialized.", LoggingLevel.Information);
            }
            catch(Exception ex)
            {
                logger.LogMessage($"Unhandled exception in BackgroundAudioTask. {ex.Message}", LoggingLevel.Critical);
                Dispose();                
            }
        }

        /// <summary>
        /// Indicate that the background task is completed.
        /// </summary>       
        private void Taskcompleted(BackgroundTaskRegistration sender, BackgroundTaskCompletedEventArgs args)
        {
            logger.LogMessage($"Background Audio Task {sender.TaskId} Completed...");
            Dispose();
        }

        /// <summary>
        /// Handles background task cancellation. Task cancellation happens due to :
        /// 1. Another Media app comes into foreground and starts playing music 
        /// 2. Resource pressure. Your task is consuming more CPU and memory than allowed.
        /// In either case, save state so that if foreground app resumes it can know where to start.
        /// </summary>
        private void OnCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            logger.LogMessage($"Background Audio Task {sender.Task.TaskId} Cancel requested because of {reason}.", LoggingLevel.Information);
            Dispose();
        }

        #endregion

        #region Background Media Player Handlers

        /// <summary>
        /// Fires when a message is received from the foreground app
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BackgroundMediaPlayer_MessageReceivedFromForeground(object sender, MediaPlayerDataReceivedEventArgs e)
        {
            foreach (string key in e.Data.Keys)
            {
                switch (key)
                {
                    case Constants.StartPlayback:
                        logger.LogMessage("BackgroundAudioTask: Starting Playback");
                        audioManager.StartPlayback();
                        break;
                    case Constants.SkipNext:
                        logger.LogMessage("BackgroundAudioTask: Skipping to next");
                        audioManager.SkipToNext();
                        break;
                    case Constants.SkipPrevious:
                        logger.LogMessage("BackgroundAudioTask: Skipping to previous");
                        audioManager.SkipToPrevious();
                        break;
                    case Constants.PausePlayback:
                        logger.LogMessage("BackgroundAudioTask: Trying to pause playback");
                        audioManager.PausePlayback();
                        break;
                }
            }
        }

        #endregion

        #region IDisposable Support

        private void Dispose(bool disposing)
        {
            if (isDisposed || !disposing)
            {
                return;
            }

            try
            {
                applicationSettingsHelper?.SaveSettingsValue(Constants.IsBackgroundTaskRunning, false);
                audioManager?.Dispose();
                logger?.SaveLogsToFile();
                BackgroundMediaPlayer.Shutdown();
            }
            catch (Exception ex)
            {
                logger?.LogMessage($"BackgroundAudioTask: Error when disposing. {ex.Message}", LoggingLevel.Error);
            }
            finally
            {
                deferral.Complete();
            }

            logger?.LogMessage($"BackgroundAudioTask has been shut down correctly.");
            isDisposed = true;
        }

        /// <summary>
        /// Releases used resources
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        #endregion
    }
}
