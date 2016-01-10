namespace AsotListener.BackgroundUpdater
{
    using System.Linq;
    using Ioc;
    using Models;
    using Services.Contracts;
    using Windows.ApplicationModel.Background;
    using Windows.Foundation.Diagnostics;

    public sealed class BackgroundUpdaterTask : IBackgroundTask
    {
        private BackgroundTaskDeferral deferral;
        private ILogger logger;

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            deferral = taskInstance.GetDeferral();
            Services.IoC.Register();
            var container = Container.Instance;
            logger = container.Resolve<ILogger>();

            taskInstance.Task.Completed += onTaskCompleted;
            taskInstance.Canceled += onTaskCanceled;
            logger.LogMessage("BackgroundUpdater: Task initialized.", LoggingLevel.Information);

            var episodeListManager = container.Resolve<IEpisodeListManager>();
            await episodeListManager.Initialization;
            var oldEpisodeList = EpisodeList.Instance.ToList();
            await episodeListManager.LoadEpisodeListFromServer();
            var diff = EpisodeList.Instance.Except(oldEpisodeList).ToList();
            if (diff.Any())
            {
                var downloadManager = container.Resolve<IDownloadManager>();
                await downloadManager.Initialization;
                foreach (var episode in diff)
                {
                    downloadManager.ScheduleDownload(episode);
                }
            }

            deferral.Complete();
        }

        private void onTaskCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            logger.LogMessage($"BackgroundUpdater: Task cancelled. Reason: {reason}", LoggingLevel.Warning);
            deferral.Complete();
        }

        private void onTaskCompleted(BackgroundTaskRegistration sender, BackgroundTaskCompletedEventArgs args)
        {
            logger.LogMessage("BackgroundUpdater: Task completed.", LoggingLevel.Information);
            deferral.Complete();
        }
    }
}
