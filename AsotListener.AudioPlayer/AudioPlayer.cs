namespace AsotListener.AudioPlayer
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using Windows.ApplicationModel.Background;
    using Windows.Media;
    using Windows.Media.Playback;
    using Windows.Foundation.Collections;
    using Models;
    using Services;

    /* This is the Sample background task that will start running the first time 
 * MediaPlayer singleton instance is accessed from foreground. When a new audio 
 * or video app comes into picture the task is expected to recieve the cancelled 
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
    public sealed class AudioPlayer : IBackgroundTask, IDisposable
    {
        #region Private fields

        private AudioManager audioManager;
        private BackgroundTaskDeferral deferral; // Used to keep task alive
        private ForegroundAppStatus foregroundAppState = ForegroundAppStatus.Unknown;
        private AutoResetEvent BackgroundTaskStarted = new AutoResetEvent(false);
        private bool backgroundtaskrunning = false;
        private IApplicationSettingsHelper applicationSettingsHelper;

        #endregion

        #region IBackgroundTask and IBackgroundTaskInstance Interface Members and handlers
        
        /// <summary>
        /// The Run method is the entry point of a background task. 
        /// </summary>
        /// <param name="taskInstance"></param>
        public void Run(IBackgroundTaskInstance taskInstance)
        {
            Debug.WriteLine("Background Audio Task " + taskInstance.Task.Name + " starting...");

            audioManager = new AudioManager(
                Playlist.Instance,
                BackgroundMediaPlayer.Current,
                SystemMediaTransportControls.GetForCurrentView(),
                waitForTaskReinitialization);

            // Associate a cancellation and completed handlers with the background task.
            taskInstance.Canceled += new BackgroundTaskCanceledEventHandler(OnCanceled);
            taskInstance.Task.Completed += Taskcompleted;

            applicationSettingsHelper = ApplicationSettingsHelper.Instance;
            var value = applicationSettingsHelper.ReadSettingsValue(Constants.AppState);
            if (value == null)
            {
                foregroundAppState = ForegroundAppStatus.Unknown;
            }
            else
            {
                foregroundAppState = (ForegroundAppStatus)Enum.Parse(typeof(ForegroundAppStatus), value.ToString());
            }

            //Initialize message channel 
            BackgroundMediaPlayer.MessageReceivedFromForeground += BackgroundMediaPlayer_MessageReceivedFromForeground;
            audioManager.CurrentTrackChanged += playList_TrackChanged;

            //Send information to foreground that background task has been started if app is active
            if (foregroundAppState != ForegroundAppStatus.Suspended)
            {
                ValueSet message = new ValueSet() { { Constants.BackgroundTaskStarted, "" } };
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
            Debug.WriteLine("MyBackgroundAudioTask " + sender.TaskId + " Completed...");
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
            Debug.WriteLine("MyBackgroundAudioTask " + sender.Task.TaskId + " Cancel Requested...");
            this.Dispose();
            deferral.Complete(); // signals task completion. 
            Debug.WriteLine("MyBackgroundAudioTask Cancel complete...");
        }

        private void waitForTaskReinitialization()
        {
            if (!backgroundtaskrunning)
            {
                bool result = BackgroundTaskStarted.WaitOne(2000);
                if (!result)
                {
                    throw new Exception("Background Task didnt initialize in time");
                }
            }
        }

        #endregion

        #region Playlist management functions and handlers

        /// <summary>
        /// Fires when playlist changes to a new track
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        void playList_TrackChanged(object sender, TrackChangedEventArgs args)
        {
            if (foregroundAppState == ForegroundAppStatus.Active)
            {
                //Message channel that can be used to send messages to foreground
                ValueSet message = new ValueSet() { { Constants.Trackchanged, args.Track } };
                BackgroundMediaPlayer.SendMessageToForeground(message);
            }
        }

        #endregion

        #region Background Media Player Handlers
        /// <summary>
        /// Fires when a message is recieved from the foreground app
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void BackgroundMediaPlayer_MessageReceivedFromForeground(object sender, MediaPlayerDataReceivedEventArgs e)
        {
            foreach (string key in e.Data.Keys)
            {
                switch (key.ToLower())
                {
                    case Constants.AppSuspended:
                        Debug.WriteLine("App suspending"); // App is suspended, you can save your task state at this point
                        foregroundAppState = ForegroundAppStatus.Suspended;
                        audioManager.SaveCurrentState();
                        break;
                    case Constants.AppResumed:
                        Debug.WriteLine("App resuming"); // App is resumed, now subscribe to message channel
                        foregroundAppState = ForegroundAppStatus.Active;
                        break;
                    case Constants.StartPlayback: //Foreground App process has signalled that it is ready for playback
                        Debug.WriteLine("Starting Playback");
                        audioManager.StartPlayback();
                        break;
                    case Constants.SkipNext: // User has chosen to skip track from app context.
                        Debug.WriteLine("Skipping to next");
                        audioManager.SkipToNext();
                        break;
                    case Constants.SkipPrevious: // User has chosen to skip track from app context.
                        Debug.WriteLine("Skipping to previous");
                        audioManager.SkipToPrevious();
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
                        audioManager.SaveCurrentState();
                        applicationSettingsHelper.SaveSettingsValue(Constants.BackgroundTaskState, Constants.BackgroundTaskCancelled);
                        applicationSettingsHelper.SaveSettingsValue(Constants.AppState, Enum.GetName(typeof(ForegroundAppStatus), foregroundAppState));
                        backgroundtaskrunning = false;

                        BackgroundTaskStarted.Dispose();
                        audioManager.Dispose();
                        BackgroundMediaPlayer.Shutdown(); // shutdown media pipeline
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.ToString());
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
