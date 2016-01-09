namespace AsotListener.BackgroundUpdater
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Common;
    using Ioc;
    using Models;
    using Models.Enums;
    using Services.Contracts;
    using Windows.ApplicationModel.Background;
    using Windows.Foundation.Diagnostics;
    public sealed class BackgroundUpdaterTask : IBackgroundTask
    {
        private BackgroundTaskDeferral deferral;
        private ILogger logger;

        public void Run(IBackgroundTaskInstance taskInstance)
        {
            deferral = taskInstance.GetDeferral();
            var container = Container.Instance;
            logger = container.Resolve<ILogger>();

            taskInstance.Task.Completed += onTaskCompleted;
            taskInstance.Canceled += onTaskCanceled;
            logger.LogMessage("BackgroundUpdater: Task initialized.", LoggingLevel.Information);

            // TODO: Implement task logic.

            deferral.Complete();
        }

        private void onTaskCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            logger.LogMessage($"BackgroundUpdater: Task cancelled. Reason: {reason}", LoggingLevel.Warning);
        }

        private void onTaskCompleted(BackgroundTaskRegistration sender, BackgroundTaskCompletedEventArgs args)
        {
            logger.LogMessage("BackgroundUpdater: Task completed.", LoggingLevel.Information);
        }
    }
}
