namespace AsotListener.ViewModels
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

    [DataContract]
    public class MainPageViewModel: BaseModel
    {
        private ObservableCollection<Episode> episodes = new ObservableCollection<Episode>();
        private readonly ILoggingSession loggingSession;

        //private readonly PlayerViewModel playerViewModel = new PlayerViewModel();

        [DataMember]
        public ObservableCollection<Episode> Episodes
        {
            get { return episodes; }
            private set { SetField(ref episodes, value, nameof(Episodes)); } 
        }

        public MainPageViewModel(ILoggingSession loggingSession)
        {
            this.loggingSession = loggingSession;
            this.RefreshCommand = new RelayCommand(() => { loadEpisodeListFromServer(); });
        }

        public ICommand RefreshCommand { get; private set; }

        /// <summary>
        /// Populates the page with content passed during navigation. Any saved state is also
        /// provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="sender">
        /// The source of the event; typically <see cref="NavigationHelper"/>.
        /// </param>
        /// <param name="e">Event data that provides both the navigation parameter passed to
        /// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested and
        /// a dictionary of state preserved by this page during an earlier
        /// session. The state will be null the first time a page is visited.</param>
        public async void OnNavigationHelperLoadState(object sender, LoadStateEventArgs e)
        {
            // TODO: Add logging here

            if (e.PageState != null && e.PageState.ContainsKey(nameof(Episodes)))
            {
                this.Episodes = (ObservableCollection<Episode>)e.PageState[nameof(Episodes)];
                return;
            }

            await loadEpisodeListFromServer();
        }

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

        /// <summary>
        /// Preserves state associated with this page in case the application is suspended or the
        /// page is discarded from the navigation cache. Values must conform to the serialization
        /// requirements of <see cref="SuspensionManager.SessionState"/>.
        /// </summary>
        /// <param name="sender">The source of the event; typically <see cref="NavigationHelper"/>.</param>
        /// <param name="e">Event data that provides an empty dictionary to be populated with
        /// serializable state.</param>
        public void OnNavigationHelperSaveState(object sender, SaveStateEventArgs e)
        {
            if (null != this.Episodes)
            {
                e.PageState[nameof(Episodes)] = this.Episodes;
            }
        }

        #region Click Handlers

        public void Download_AppBarButton_Click(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        public void Remove_AppBarButton_Click(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        public void Play_AppBarButton_Click(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        public void ItemView_ItemClick(object sender, ItemClickEventArgs e)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
