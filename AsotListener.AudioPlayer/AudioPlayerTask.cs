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
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable")]
    public sealed class AudioPlayerTask : IBackgroundTask
    {
        #region Private fields

        private bool isCleanedUp = false;
        private AudioManager audioManager;
        private BackgroundTaskDeferral deferral; // Used to keep task alive
        private ForegroundAppStatus foregroundAppState = ForegroundAppStatus.Unknown;
        private AutoResetEvent BackgroundTaskStartedEvent = new AutoResetEvent(false);
        private bool backgroundtaskrunning = false;
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

            var playlist = container.Resolve<IPlayList>();
            await playlist.LoadPlaylistFromLocalStorage();

            audioManager = new AudioManager(
                logger,
                playlist,
                BackgroundMediaPlayer.Current,
                SystemMediaTransportControls.GetForCurrentView(),
                waitForTaskReinitialization);

            // Associate a cancellation and completed handlers with the background task.
            taskInstance.Canceled += new BackgroundTaskCanceledEventHandler(OnCanceled);
            taskInstance.Task.Completed += Taskcompleted;

            string value = applicationSettingsHelper.ReadSettingsValue<string>(Constants.AppState);
            foregroundAppState = ForegroundAppStatus.Unknown;
            if (!string.IsNullOrEmpty(value))
            {
                Enum.TryParse(value, out foregroundAppState);
            }

            //Initialize message channel 
            BackgroundMediaPlayer.MessageReceivedFromForeground += BackgroundMediaPlayer_MessageReceivedFromForeground;

            //Send information to foreground that background task has been started if app is active
            if (foregroundAppState != ForegroundAppStatus.Suspended)
            {
                ValueSet message = new ValueSet() { { Constants.BackgroundTaskStarted, string.Empty } };
                BackgroundMediaPlayer.SendMessageToForeground(message);
            }
            BackgroundTaskStartedEvent.Set();
            backgroundtaskrunning = true;

            applicationSettingsHelper.SaveSettingsValue(Constants.BackgroundTaskState, Constants.BackgroundTaskRunning);
        }

        /// <summary>
        /// Indicate that the background task is completed.
        /// </summary>       
        void Taskcompleted(BackgroundTaskRegistration sender, BackgroundTaskCompletedEventArgs args)
        {
            logger.LogMessage($"Background Audio Task {sender.TaskId} Completed...");
            cleanUp();
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
            cleanUp();
            deferral.Complete();
        }

        private void waitForTaskReinitialization()
        {
            if (!backgroundtaskrunning)
            {
                logger.LogMessage("Background Audio Task: It seems the task is not running. Waiting for it to start.");
                bool result = BackgroundTaskStartedEvent.WaitOne(Constants.BackgroundAudioWaitingTime);
                if (!result)
                {
                    const string message = "Background Task didn't initialize in time.";
                    logger.LogMessage(message, LoggingLevel.Error);
                    throw new Exception(message);
                }
            }
        }

        private void cleanUp()
        {
            if (isCleanedUp)
            {
                return;
            }

            try
            {
                applicationSettingsHelper.SaveSettingsValue(Constants.BackgroundTaskState, Constants.BackgroundTaskCancelled);
                applicationSettingsHelper.SaveSettingsValue(Constants.AppState, Enum.GetName(typeof(ForegroundAppStatus), foregroundAppState));
                backgroundtaskrunning = false;

                BackgroundTaskStartedEvent.Dispose();
                audioManager.Dispose();
                BackgroundMediaPlayer.Shutdown();
                logger.SaveLogsToFile();
                isCleanedUp = true;
            }
            catch (Exception ex)
            {
                logger.LogMessage($"Error when disposing background audio task. {ex.Message}", LoggingLevel.Error);
            }

            logger.LogMessage($"Background Audio Task has been shut down correctly.");
        }

        #endregion

        #region Background Media Player Handlers

        /// <summary>
        /// Fires when a message is received from the foreground app
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void BackgroundMediaPlayer_MessageReceivedFromForeground(object sender, MediaPlayerDataReceivedEventArgs e)
        {
            foreach (string key in e.Data.Keys)
            {
                switch (key)
                {
                    case Constants.AppSuspended:
                        logger.LogMessage("BackgroundAudioTask: App suspending"); // App is suspended, you can save your task state at this point
                        foregroundAppState = ForegroundAppStatus.Suspended;
                        audioManager.SaveCurrentState();
                        break;
                    case Constants.AppResumed:
                        logger.LogMessage("BackgroundAudioTask: App resuming"); // App is resumed, now subscribe to message channel
                        foregroundAppState = ForegroundAppStatus.Active;
                        break;
                    case Constants.StartPlayback: //Foreground App process has signaled that it is ready for playback
                        logger.LogMessage("BackgroundAudioTask: Starting Playback");
                        audioManager.StartPlayback();
                        break;
                    case Constants.SkipNext: // User has chosen to skip track from app context.
                        logger.LogMessage("BackgroundAudioTask: Skipping to next");
                        audioManager.SkipToNext();
                        break;
                    case Constants.SkipPrevious: // User has chosen to skip track from app context.
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
    }
}
