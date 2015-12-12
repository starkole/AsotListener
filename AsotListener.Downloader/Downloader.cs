namespace AsotListener.Downloader
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Windows.ApplicationModel.Background;
    using Services.Contracts;
    using Services.Implementations;
    using Models;

    public sealed class Downloader : IBackgroundTask, IDisposable
    {
        private IDownloadList downloads;
        private ILoaderFactory loaderFactory;
        private ILogger logger;
        private IFileUtils fileUtils;
        private IParser parser;

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            var deferral = taskInstance.GetDeferral();

            downloads = DownloadList.Instance;
            logger = Logger.Instance;
            parser = Parser.Instance;
            fileUtils = FileUtils.Instance;
            loaderFactory = new LoaderFactory(logger, fileUtils);

            logger.LogMessage("Background downloader started.");

            while (downloads.Any())
            {
                Episode episode = null;
                bool operationResult = false;
                while (downloads.Any() && !operationResult)
                {
                    operationResult = downloads.TryGetFirst(out episode);
                }

                if (episode == null)
                {
                    break;
                }

                await downloadEpisode(episode);
            }

            logger.LogMessage("Background downloader - exiting.");
            deferral.Complete();
        }

        private async Task downloadEpisode(Episode episode)
        {
            using (ILoader loader = loaderFactory.GetLoader())
            {
                string episodePage = await loader.FetchEpisodePageAsync(episode);
                episode.DownloadLinks = parser.ExtractDownloadLinks(episodePage);
                episode.Status = EpisodeStatus.Downloading;
                await loader.DownloadEpisodeAsync(episode);
                episode.Status = EpisodeStatus.Loaded;
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
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
