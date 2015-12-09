namespace AsotListener.App.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Threading.Tasks;
    using Services;
    using Models;
    using System.Windows.Input;
    using System.Runtime.Serialization;
    using System.IO;
    using Windows.Storage;
    using Windows.Foundation.Diagnostics;
    using Windows.Storage.Streams;

    public class EpisodesViewModel : BaseModel, IDisposable
    {
        #region Fields

        private const string episodeListFileName = "episodeList.xml";
        private ObservableCollection<Episode> episodes;
        private ILogger logger;
        private readonly IFileUtils fileUtils;

        #endregion

        #region Properties

        public ObservableCollection<Episode> EpisodeList
        {
            get { return episodes; }
            private set { SetField(ref episodes, value, nameof(EpisodeList)); }
        }

        public ICommand RefreshCommand { get; }
        public ICommand DownloadCommand { get; }
        public ICommand CancelDownloadCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand PlayCommand { get; }
        public ICommand AddToPlaylistCommand { get; }

        #endregion

        #region Ctor

        public EpisodesViewModel(ILogger logger, IFileUtils fileUtils)
        {
            this.logger = logger;
            this.fileUtils = fileUtils;

            this.RefreshCommand = new RelayCommand(loadEpisodeListFromServer);
            this.DownloadCommand = new RelayCommand(downloadEpisode);
            this.CancelDownloadCommand = new RelayCommand((Action<object>)cancelDownload);
            this.DeleteCommand = new RelayCommand((Action<object>)deleteEpisodeFromStorage);
            this.PlayCommand = new RelayCommand((Action<object>)playEpisode);
            this.AddToPlaylistCommand = new RelayCommand((Action<object>)addEpisodeToPlaylist);

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            RestoreEpisodesList();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        }

        #endregion

        #region Public Methods

        public async Task RestoreEpisodesList()
        {
            try
            {
                logger.LogMessage("Reading episode list from file...");
                StorageFile file = await ApplicationData.Current.LocalFolder.GetFileAsync(episodeListFileName);
                using (IInputStream inStream = await file.OpenSequentialReadAsync())
                {
                    // Deserialize the Session State
                    DataContractSerializer serializer = new DataContractSerializer(typeof(ObservableCollection<Episode>));
                    EpisodeList = serializer.ReadObject(inStream.AsStreamForRead()) as ObservableCollection<Episode>;
                }
                logger.LogMessage("Episode list has been successfully read from file.");
            }
            catch (Exception e)
            {
                logger.LogMessage($"Error reading episodes list. {e.Message}", LoggingLevel.Error);
            }

            if (EpisodeList == null || !EpisodeList.Any())
            {
                await loadEpisodeListFromServer();
            }
            else
            {
                await updateEpisodesStates();
            }
        }

        public async Task StoreEpisodesList()
        {
            try
            {
                logger.LogMessage("Saving episode list to file...");
                MemoryStream listData = new MemoryStream();
                DataContractSerializer serializer = new DataContractSerializer(typeof(ObservableCollection<Episode>));
                serializer.WriteObject(listData, EpisodeList);

                StorageFile file = await ApplicationData.Current.LocalFolder.CreateFileAsync(episodeListFileName, CreationCollisionOption.ReplaceExisting);
                using (Stream fileStream = await file.OpenStreamForWriteAsync())
                {
                    listData.Seek(0, SeekOrigin.Begin);
                    await listData.CopyToAsync(fileStream);
                }

                logger.LogMessage("Episode list has been saved.");
            }
            catch (Exception e)
            {
                logger.LogMessage($"Cannot save episodes list. {e.Message}", LoggingLevel.Error);
            }
        }

        #endregion

        #region Commands

        private async Task loadEpisodeListFromServer()
        {
            using (Loader loader = new Loader(logger, fileUtils))
            {
                string episodeListPage = await loader.FetchEpisodeListAsync();
                this.EpisodeList = Parser.ParseEpisodeList(episodeListPage);
            }
            await updateEpisodesStates();
            await StoreEpisodesList();
        }

        private async Task downloadEpisode(object boxedEpisode)
        {
            var episode = boxedEpisode as Episode;
            if (episode == null)
            {
                return;
            }

            // TODO: Implement loading queue

            using (Loader loader = new Loader(logger, fileUtils))
            {
                string episodePage = await loader.FetchEpisodePageAsync(episode);
                episode.DownloadLinks = Parser.ExtractDownloadLinks(episodePage);
                episode.Status = EpisodeStatus.Downloading;
                await loader.DownloadEpisodeAsync(episode);
                episode.Status = EpisodeStatus.Loaded;
            }
        }

        private void cancelDownload(object boxedEpisode)
        {
            var episode = boxedEpisode as Episode;
            if (episode == null)
            {
                return;
            }

            // TODO: Implement command logic
        }

        private void deleteEpisodeFromStorage(object boxedEpisode)
        {
            var episode = boxedEpisode as Episode;
            if (canEpisodeBeDeleted(episode))
            {
                fileUtils.DeleteEpisode(episode.Name);
            }
        }

        private void playEpisode(object boxedEpisode)
        {
            var episode = boxedEpisode as Episode;
            if (episode == null)
            {
                return;
            }

            // TODO: Implement command logic
        }

        private void addEpisodeToPlaylist(object boxedEpisode)
        {
            var episode = boxedEpisode as Episode;
            if (episode == null)
            {
                return;
            }

            // TODO: Implement command logic
        }

        #endregion

        #region Private Methods

        private async Task updateEpisodesStates()
        {
            if (this.EpisodeList == null || !this.EpisodeList.Any())
            {
                return;
            }

            var existingFileNames = await fileUtils.GetDownloadedFileNamesList();
            foreach (Episode episode in this.EpisodeList)
            {
                if (existingFileNames.Contains(episode.Name))
                {
                    episode.Status = EpisodeStatus.Loaded;
                }
            }
        }

        private bool canEpisodeBeDeleted(Episode episode) =>
            episode != null &&
            episode.Status == EpisodeStatus.Loaded &&
            episode.Status == EpisodeStatus.Playing;

        #endregion

        #region IDisposable Support

        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
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
