namespace AsotListener.AudioPlayer
{
    using System;
    using System.Threading;
    using Windows.ApplicationModel.Background;
    using Windows.Media;
    using Windows.Media.Playback;
    using Windows.Foundation.Collections;
    using Models;
    using Services.Contracts;
    using Windows.Foundation.Diagnostics;
    using Ioc;
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
        private ForegroundAppStatus foregroundAppState = ForegroundAppStatus.Unknown;
        private AutoResetEvent BackgroundTaskStartedEvent = new AutoResetEvent(false);
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

            IContainer container = Container.Instance;
            Services.IoC.Register();

            logger = container.Resolve<ILogger>();
            logger.LogMessage($"Background Audio Task {taskInstance.Task.Name} starting...");
            applicationSettingsHelper = container.Resolve<IApplicationSettingsHelper>();
            IPlayList playlist = container.Resolve<IPlayList>();
            await playlist.LoadPlaylistFromLocalStorage();

            audioManager = new AudioManager(
                logger,
                playlist,
                BackgroundMediaPlayer.Current,
                SystemMediaTransportControls.GetForCurrentView(),
                waitForTaskReinitialization);

            taskInstance.Canceled += new BackgroundTaskCanceledEventHandler(OnCanceled);
            taskInstance.Task.Completed += Taskcompleted;
            BackgroundMediaPlayer.MessageReceivedFromForeground += BackgroundMediaPlayer_MessageReceivedFromForeground;

            string value = applicationSettingsHelper.ReadSettingsValue<string>(Constants.AppState);
            foregroundAppState = ForegroundAppStatus.Unknown;
            Enum.TryParse(value, out foregroundAppState);
            if (foregroundAppState != ForegroundAppStatus.Suspended)
            {
                ValueSet message = new ValueSet() { { Constants.BackgroundTaskStarted, string.Empty } };
                BackgroundMediaPlayer.SendMessageToForeground(message);
            }
            BackgroundTaskStartedEvent.Set();
            applicationSettingsHelper.SaveSettingsValue(Constants.BackgroundTaskState, Constants.BackgroundTaskRunning);
            logger.LogMessage($"Background Audio Task initialized.");
        }

        /// <summary>
        /// Indicate that the background task is completed.
        /// </summary>       
        void Taskcompleted(BackgroundTaskRegistration sender, BackgroundTaskCompletedEventArgs args)
        {
            logger.LogMessage($"Background Audio Task {sender.TaskId} Completed...");
            Dispose();
            deferral.Complete();
        }

        /// <summary>
        /// Handles background task cancellation. Task cancellation happens due to :
        /// 1. Another Media app comes into foreground and starts playing music 
        /// 2. Resource pressure. Your task is consuming more CPU and memory than allowed.
        /// In either case, save state so that if foreground app resumes it can know where to start.
        /// </summary>
        private void OnCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            logger.LogMessage($"Background Audio Task {sender.Task.TaskId} Cancel requested because of {reason}.");
            Dispose();
            deferral.Complete();
        }

        private void waitForTaskReinitialization()
        {
            logger.LogMessage("Background Audio Task: It seems the task is not running. Waiting for it to start.");
            bool result = BackgroundTaskStartedEvent.WaitOne(Constants.BackgroundAudioWaitingTime);
            if (!result)
            {
                const string message = "Background Task didn't initialize in time.";
                logger.LogMessage(message, LoggingLevel.Critical);
                throw new Exception(message);
            }
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
                    case Constants.AppSuspended:
                        logger.LogMessage("BackgroundAudioTask: Foreground app suspending");
                        foregroundAppState = ForegroundAppStatus.Suspended;
                        break;
                    case Constants.AppResumed:
                        logger.LogMessage("BackgroundAudioTask: Foreground app resuming");
                        foregroundAppState = ForegroundAppStatus.Active;

                        break;
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

        void Dispose(bool disposing)
        {
            if (isDisposed || !disposing)
            {
                return;
            }

            try
            {
                applicationSettingsHelper.SaveSettingsValue(Constants.BackgroundTaskState, Constants.BackgroundTaskCancelled);
                BackgroundTaskStartedEvent.Dispose();
                audioManager.Dispose();
                BackgroundMediaPlayer.Shutdown();
                logger.SaveLogsToFile();
            }
            catch (Exception ex)
            {
                logger.LogMessage($"Error when disposing background audio task. {ex.Message}", LoggingLevel.Error);
            }

            logger.LogMessage($"Background Audio Task has been shut down correctly.");
            isDisposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        #endregion
    }
}
