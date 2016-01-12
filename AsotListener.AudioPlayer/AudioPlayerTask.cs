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
    using Models.Enums;

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

            try
            {

                // Ensure that Background Audio is initialized by accessing BackgroundMediaPlayer.Current
                var state = BackgroundMediaPlayer.Current.CurrentState;
                logger.LogMessage($"BackgroundAudioTask BackgroundMediaPlayer state is {state}.", LoggingLevel.Information);

                applicationSettingsHelper = container.Resolve<IApplicationSettingsHelper>();
                await applicationSettingsHelper.LoadPlaylist();
                audioManager = new AudioManager(
                    logger,
                    applicationSettingsHelper,
                    SystemMediaTransportControls.GetForCurrentView());

                BackgroundMediaPlayer.MessageReceivedFromForeground -= onMessageReceivedFromForeground;
                BackgroundMediaPlayer.MessageReceivedFromForeground += onMessageReceivedFromForeground;

                applicationSettingsHelper.SaveSettingsValue(Keys.IsBackgroundTaskRunning, true);
                BackgroundMediaPlayer.SendMessageToForeground(new ValueSet { { Keys.IsBackgroundTaskRunning, null } });
                logger.LogMessage($"BackgroundAudioTask initialized.", LoggingLevel.Information);
            }
            catch (Exception ex)
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
        private async void onMessageReceivedFromForeground(object sender, MediaPlayerDataReceivedEventArgs e)
        {
            int navigationAmount = 0;
            NavigationInterval navigationInterval = NavigationInterval.Unspecified;

            foreach (string key in e.Data.Keys)
            {
                switch (key)
                {
                    case Keys.StartPlayback:
                        logger.LogMessage("BackgroundAudioTask: Starting Playback");
                        await audioManager.StartPlayback();
                        break;
                    case Keys.SkipToNext:
                        logger.LogMessage("BackgroundAudioTask: Skipping to next");
                        audioManager.SkipToNext();
                        break;
                    case Keys.SkipToPrevious:
                        logger.LogMessage("BackgroundAudioTask: Skipping to previous");
                        audioManager.SkipToPrevious();
                        break;
                    case Keys.PausePlayback:
                        logger.LogMessage("BackgroundAudioTask: Trying to pause playback");
                        audioManager.PausePlayback();
                        break;
                    case Keys.SchedulePause:
                        logger.LogMessage("BackgroundAudioTask: Scheduling the pause");
                        audioManager.SchedulePause();
                        break;
                    case Keys.PlaylistUpdated:
                        logger.LogMessage("BackgroundAudioTask: Playlist updated");
                        await audioManager.LoadState();
                        break;
                    case Keys.NavigationAmount:
                        navigationAmount = (int)e.Data[Keys.NavigationAmount];
                        logger.LogMessage($"BackgroundAudioTask: Obtained navigation amount {navigationAmount}");
                        break;
                    case Keys.NavigationInterval:
                        navigationInterval = (NavigationInterval)e.Data[Keys.NavigationInterval];
                        logger.LogMessage($"BackgroundAudioTask: Obtained navigation interval {navigationInterval}");
                        break;
                }
            }

            if (navigationAmount != 0 && navigationInterval != NavigationInterval.Unspecified)
            {
                audioManager.Navigate(navigationAmount, navigationInterval);
            }
        }

        #endregion

        #region IDisposable Support

        /// <summary>
        /// Releases used resources
        /// </summary>
        public void Dispose()
        {
            if (isDisposed)
            {
                return;
            }

            try
            {
                BackgroundMediaPlayer.MessageReceivedFromForeground -= onMessageReceivedFromForeground;
                applicationSettingsHelper?.SaveSettingsValue(Keys.IsBackgroundTaskRunning, false);
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

        #endregion
    }
}
