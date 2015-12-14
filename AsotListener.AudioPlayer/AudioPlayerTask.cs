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

    /* This is the Sample background task that will start running the first time 
 * MediaPlayer singleton instance is accessed from foreground. When a new audio 
 * or video app comes into picture the task is expected to receive the cancelled 
 * event. User can save state and shutdown MediaPlayer at that time. When foreground 
 * app is resumed or restarted check if your music is still playing or continue from
 * previous state.
 * 
 * This task also implements SystemMediaTransportControl apis for windows phone universal 
 * volume control. Unlike Windows 8.1 where there are different views in phone context, 
 * SystemMediaTransportControl is singleton in nature bound to the process in which it is 
 * initialized. If you want to hook up volume controls for the background task, do not 
 * implement SystemMediaTransportControls in foreground app process.
 */

    // TODO: Update documentation


    /// <summary>
    /// Impalements IBackgroundTask to provide an entry point for app code to be run in background. 
    /// Also takes care of handling UVC and communication channel with foreground
    /// </summary>
    public sealed class AudioPlayerTask : IBackgroundTask, IDisposable
    {
        #region Private fields

        private AudioManager audioManager;
        private BackgroundTaskDeferral deferral; // Used to keep task alive
        private ForegroundAppStatus foregroundAppState = ForegroundAppStatus.Unknown;
        private AutoResetEvent BackgroundTaskStarted = new AutoResetEvent(false);
        private bool backgroundtaskrunning = false;
        private IApplicationSettingsHelper applicationSettingsHelper;
        private ILogger logger;

        #endregion

        #region IBackgroundTask and IBackgroundTaskInstance Interface Members and handlers

        /// <summary>
        /// The Run method is the entry point of a background task. 
        /// </summary>
        /// <param name="taskInstance"></param>
        public void Run(IBackgroundTaskInstance taskInstance)
        {
            Services.IoC.Register();
            IContainer container = Container.Instance;
            logger = container.Resolve<ILogger>();
            applicationSettingsHelper = container.Resolve<IApplicationSettingsHelper>();
            logger.LogMessage($"Background Audio Task {taskInstance.Task.Name} starting...");

            audioManager = new AudioManager(
                logger,
                container.Resolve<IPlayList>(),
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
            BackgroundTaskStarted.Set();
            backgroundtaskrunning = true;

            applicationSettingsHelper.SaveSettingsValue(Constants.BackgroundTaskState, Constants.BackgroundTaskRunning);
            deferral = taskInstance.GetDeferral();
        }

        /// <summary>
        /// Indicate that the background task is completed.
        /// </summary>       
        void Taskcompleted(BackgroundTaskRegistration sender, BackgroundTaskCompletedEventArgs args)
        {
            logger.LogMessage($"Background Audio Task {sender.TaskId} Completed...");
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
            // You get some time here to save your state before process and resources are reclaimed
            logger.LogMessage($"Background Audio Task {sender.Task.TaskId} Cancel Requested.");
            audioManager.Dispose();
            deferral.Complete(); // signals task completion. 
        }

        private void waitForTaskReinitialization()
        {
            if (!backgroundtaskrunning)
            {
                logger.LogMessage("Background Audio Task: It seems the task is not running. Waiting for it to start.");
                bool result = BackgroundTaskStarted.WaitOne(Constants.BackgroundAudioWaitingTime);
                if (!result)
                {
                    const string message = "Background Task didn't initialize in time.";
                    logger.LogMessage(message, LoggingLevel.Error);
                    throw new Exception(message);
                }
            }
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
                        logger.LogMessage("App suspending"); // App is suspended, you can save your task state at this point
                        foregroundAppState = ForegroundAppStatus.Suspended;
                        audioManager.SaveCurrentState();
                        break;
                    case Constants.AppResumed:
                        logger.LogMessage("App resuming"); // App is resumed, now subscribe to message channel
                        foregroundAppState = ForegroundAppStatus.Active;
                        break;
                    case Constants.StartPlayback: //Foreground App process has signaled that it is ready for playback
                        logger.LogMessage("Starting Playback");
                        audioManager.StartPlayback();
                        break;
                    case Constants.SkipNext: // User has chosen to skip track from app context.
                        logger.LogMessage("Skipping to next");
                        audioManager.SkipToNext();
                        break;
                    case Constants.SkipPrevious: // User has chosen to skip track from app context.
                        logger.LogMessage("Skipping to previous");
                        audioManager.SkipToPrevious();
                        break;
                    case Constants.PausePlayback:
                        logger.LogMessage("Trying to pause playback");
                        audioManager.PausePlayback();
                        break;
                }
            }
        }

        #endregion

        #region IDisposable Support

        private bool disposedValue = false; // To detect redundant calls

        void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    try
                    {
                        applicationSettingsHelper.SaveSettingsValue(Constants.BackgroundTaskState, Constants.BackgroundTaskCancelled);
                        applicationSettingsHelper.SaveSettingsValue(Constants.AppState, Enum.GetName(typeof(ForegroundAppStatus), foregroundAppState));
                        backgroundtaskrunning = false;

                        BackgroundTaskStarted.Dispose();
                        audioManager.Dispose();
                        BackgroundMediaPlayer.Shutdown(); // shutdown media pipeline
                    }
                    catch (Exception ex)
                    {
                        logger.LogMessage($"Error when disposing background audio task. {ex.Message}", LoggingLevel.Error);
                    }
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
