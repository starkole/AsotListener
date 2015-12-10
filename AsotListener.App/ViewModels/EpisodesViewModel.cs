namespace AsotListener.App.ViewModels
{
    using System;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Threading.Tasks;
    using Services.Contracts;
    using Models;
    using System.Windows.Input;
    using System.Runtime.Serialization;
    using System.IO;
    using Windows.Storage;
    using Windows.Foundation.Diagnostics;
    using Windows.Storage.Streams;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml;

    public class EpisodesViewModel : BaseModel, IDisposable
    {
        #region Fields

        private const string episodeListFileName = "episodeList.xml";
        private ObservableCollection<Episode> episodes;
        private readonly ILogger logger;
        private readonly IFileUtils fileUtils;
        private readonly IPlayList playlist;
        private readonly ILoaderFactory loaderFactory;
        private readonly IParser parser;

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

        public EpisodesViewModel(
            ILogger logger, 
            IFileUtils fileUtils, 
            IPlayList playlist, 
            ILoaderFactory loaderFactory,
            IParser parser)
        {
            this.logger = logger;
            this.fileUtils = fileUtils;
            this.playlist = playlist;
            this.loaderFactory = loaderFactory;
            this.parser = parser;

            RefreshCommand = new RelayCommand(loadEpisodeListFromServer);
            DownloadCommand = new RelayCommand(downloadEpisode);
            CancelDownloadCommand = new RelayCommand((Action<object>)cancelDownload);
            DeleteCommand = new RelayCommand((Action<object>)deleteEpisodeFromStorage);
            PlayCommand = new RelayCommand((Action<object>)playEpisode);
            AddToPlaylistCommand = new RelayCommand((Action<object>)addToPlaylistCommand);

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            RestoreEpisodesList();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        }

        #endregion

        #region Public Methods

        public async Task RestoreEpisodesList()
        {
            EpisodeList = await fileUtils.ReadFromXmlFile<ObservableCollection<Episode>>(episodeListFileName);
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
            await fileUtils.SaveToXmlFile(EpisodeList, episodeListFileName);
        }

        #endregion

        #region Commands

        private async Task loadEpisodeListFromServer()
        {
            using (ILoader loader = loaderFactory.GetLoader())
            {
                string episodeListPage = await loader.FetchEpisodeListAsync();
                EpisodeList = parser.ParseEpisodeList(episodeListPage);
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

            using (ILoader loader = loaderFactory.GetLoader())
            {
                string episodePage = await loader.FetchEpisodePageAsync(episode);
                episode.DownloadLinks = parser.ExtractDownloadLinks(episodePage);
                episode.Status = EpisodeStatus.Downloading;
                await loader.DownloadEpisodeAsync(episode);
                episode.Status = EpisodeStatus.Loaded;
            }
        }

        private void cancelDownload(object boxedEpisode)
        {
            var episode = boxedEpisode as Episode;
            if (episode == null && episode.Status != EpisodeStatus.Downloading)
            {
                return;
            }

            // TODO: Implement command logic
            // TODO: Don't forget to update status.
        }

        private async void deleteEpisodeFromStorage(object boxedEpisode)
        {
            var episode = boxedEpisode as Episode;
            if (canEpisodeBeDeleted(episode))
            {
                await fileUtils.DeleteEpisode(episode.Name);
            }
            episode.Status = EpisodeStatus.CanBeLoaded;
        }

        private async void playEpisode(object boxedEpisode)
        {
            var episode = boxedEpisode as Episode;
            if (episode == null && episode.Status != EpisodeStatus.Loaded)
            {
                return;
            }

            var existingTracks = playlist.TrackList.Where(t => t.Name.StartsWith(episode.Name));
            playlist.CurrentTrack = existingTracks.Any() ?
                existingTracks.First() :
                await addEpisodeToPlaylist(episode);
            await playlist.SavePlaylistToLocalStorage();

            // Navigate to player and start playback
            // TODO: This is ugly and should be replaced with some NavigationService
            Frame rootFrame = Window.Current.Content as Frame;
            rootFrame.Navigate(typeof(MainPage), Constants.StartPlayback);
        }

        private async void addToPlaylistCommand(object boxedEpisode)
        {
            var episode = boxedEpisode as Episode;
            if (episode == null && episode.Status != EpisodeStatus.Loaded)
            {
                return;
            }

            var existingTracks = playlist.TrackList.Where(t => t.Name.StartsWith(episode.Name));
            if (existingTracks.Any())
            {
                return;
            }

            await addEpisodeToPlaylist(episode);
            await playlist.SavePlaylistToLocalStorage();
        }

        #endregion

        #region Private Methods

        private async Task<AudioTrack> addEpisodeToPlaylist(Episode episode)
        {
            var fileNames = await fileUtils.GetFilesListForEpisode(episode.Name);
            int counter = 0;
            AudioTrack firstAddedTrack = null;
            foreach (var file in fileNames)
            {
                counter++;
                var track = new AudioTrack()
                {
                    Name = episode.Name + " Part " + counter.ToString(),
                    Uri = file.Path
                };

                if (counter == 1)
                {
                    firstAddedTrack = track;
                }
            }

            return firstAddedTrack;
        }

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
            (episode.Status == EpisodeStatus.Loaded ||
            episode.Status == EpisodeStatus.Playing);

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
