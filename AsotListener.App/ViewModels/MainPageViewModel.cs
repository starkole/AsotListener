namespace AsotListener.App.ViewModels
{
    using Services;
    using Services.Navigation;
    using Models;
    using System.Collections.ObjectModel;
    using System;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using System.Runtime.Serialization;
    using Windows.Foundation.Diagnostics;
    using System.Threading.Tasks;
    using System.Windows.Input;
    using System.Collections.Generic;

    [DataContract]
    public class MainPageViewModel : BaseModel, IDisposable
    {
        #region Fields

        private ObservableCollection<Episode> episodes = new ObservableCollection<Episode>();
        private Episode selectedEpisode;
        private readonly ILoggingSession loggingSession;
        private readonly IApplicationSettingsHelper applicationSettingsHelper;

        #endregion

        #region Properties

        [DataMember]
        public ObservableCollection<Episode> Episodes
        {
            get { return episodes; }
            private set { SetField(ref episodes, value, nameof(Episodes)); }
        }

        [DataMember]
        public Episode SelectedEpisode
        {
            get { return selectedEpisode; }
            set { SetField(ref selectedEpisode, value, nameof(SelectedEpisode)); }
        }

        public ICommand RefreshCommand { get; }
        public ICommand DownloadCommand { get; }

        #endregion

        #region Ctor

        public MainPageViewModel(
            ILoggingSession loggingSession,
            IApplicationSettingsHelper applicationSettingsHelper)
        {
            this.loggingSession = loggingSession;
            this.applicationSettingsHelper = applicationSettingsHelper;
            this.RefreshCommand = new RelayCommand(loadEpisodeListFromServer);
            this.DownloadCommand = new RelayCommand(downloadSelectedEpisode);

            App.Current.Suspending += onAppSuspending;
            App.Current.Resuming += onAppResuming;
        }

        #endregion


        #region State Management

        private async void onAppResuming(object sender, object eventArgs)
        {
            Episodes = applicationSettingsHelper.ReadSettingsValue(Constants.EpisodesList) as ObservableCollection<Episode>;

            if (Episodes == null)
            {
                await loadEpisodeListFromServer();
            }
        }

        private void onAppSuspending(object sender, Windows.ApplicationModel.SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            applicationSettingsHelper.SaveSettingsValue(Constants.EpisodesList, Episodes);
            deferral.Complete();
        }

        #endregion

        #region Private Methods

        private async Task loadEpisodeListFromServer()
        {
            using (Loader loader = new Loader(this.loggingSession))
            {
                string episodeListPage = await loader.FetchEpisodeListAsync();
                this.Episodes = Parser.ParseEpisodeList(episodeListPage);
            }

            await updateEpisodesStates();
        }

        private async Task updateEpisodesStates()
        {
            if (this.Episodes == null || this.Episodes.Count < 1)
            {
                return;
            }

            var existingFileNames = await FileManager.GetDownloadedFileNamesList();
            foreach (Episode episode in this.Episodes)
            {
                if (existingFileNames.Contains(episode.Name))
                {
                    episode.Status = EpisodeStatus.Loaded;
                }
            }
        }

        private async Task downloadSelectedEpisode()
        {
            if (SelectedEpisode == null)
            {
                return;
            }

            // TODO: Implement loading queue

            using (Loader loader = new Loader(this.loggingSession))
            {
                var episode = SelectedEpisode;
                string episodePage = await loader.FetchEpisodePageAsync(episode);

                // TODO: Store links inside episode
                List<string> urls = Parser.ExtractDownloadLinks(episodePage);
                episode.Status = EpisodeStatus.Downloading;
                await loader.DownloadEpisodeAsync(episode, urls);
                episode.Status = EpisodeStatus.Loaded;
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
                    applicationSettingsHelper.SaveSettingsValue(Constants.EpisodesList, Episodes);
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
