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

    public class EpisodesViewModel: BaseModel, IDisposable
    {
        #region Fields

        private ObservableCollection<Episode> episodes;
        private ILogger logger;

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

        public EpisodesViewModel(ILogger logger)
        {
            this.logger = logger;

            this.RefreshCommand = new RelayCommand(loadEpisodeListFromServer);
            this.DownloadCommand = new RelayCommand(downloadEpisode);
            this.CancelDownloadCommand = new RelayCommand((Action<object>)cancelDownload);
            this.DeleteCommand = new RelayCommand((Action<object>)deleteEpisodeFromStorage);
            this.PlayCommand = new RelayCommand((Action<object>)playEpisode);
            this.AddToPlaylistCommand = new RelayCommand((Action<object>)addEpisodeToPlaylist);

            RestoreEpisodesList(); // TODO: Figure out something here
        }

        #endregion

        #region Public Methods

        public async Task RestoreEpisodesList()
        {
            // TODO: Read list from file here
            // await updateEpisodesStates();

            if (EpisodeList == null || !EpisodeList.Any())
            {
                await loadEpisodeListFromServer();
            }
        }

        public async Task StoreEpisodesList()
        {
            // TODO: Store episode list to file here
        }

        #endregion

        #region Commands

        private async Task loadEpisodeListFromServer()
        {
            using (Loader loader = new Loader(this.logger))
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

            using (Loader loader = new Loader(this.logger))
            {
                string episodePage = await loader.FetchEpisodePageAsync(episode);

                // TODO: Store links inside episode
                List<string> urls = Parser.ExtractDownloadLinks(episodePage);
                episode.Status = EpisodeStatus.Downloading;
                await loader.DownloadEpisodeAsync(episode, urls);
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
            if (episode == null)
            {
                return;
            }

            // TODO: Implement command logic
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

            var existingFileNames = await FileManager.GetDownloadedFileNamesList();
            foreach (Episode episode in this.EpisodeList)
            {
                if (existingFileNames.Contains(episode.Name))
                {
                    episode.Status = EpisodeStatus.Loaded;
                }
            }
        }
        
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
