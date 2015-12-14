namespace AsotListener.App.ViewModels
{
    using Windows.UI.Xaml;
    using Models;
    using Services.Contracts;

    public class MainPageViewModel : BaseModel
    {
        #region Fields

        private readonly ILogger logger;

        #endregion

        #region Properties

        public PlayerViewModel PlayerModel { get; }
        public EpisodesViewModel EpisodesModel { get; }


        #endregion

        #region Ctor

        public MainPageViewModel(PlayerViewModel playerModel, EpisodesViewModel episodesViewModel, ILogger logger)
        {
            PlayerModel = playerModel;
            EpisodesModel = episodesViewModel;
            this.logger = logger;

            Application.Current.Suspending += onAppSuspending;
            Application.Current.Resuming += onAppResuming;
            logger.LogMessage("Main page view model created.");
        }

        #endregion

        #region State Management

        private async void onAppResuming(object sender, object eventArgs)
        {
            logger.LogMessage("Application resuming. Loading view models state...");
            // TODO: Do I really need this here?
            await EpisodesModel.RestoreEpisodesList();
            await PlayerModel.Playlist.LoadPlaylistFromLocalStorage();
            logger.LogMessage("View models state loaded.");
        }

        private async void onAppSuspending(object sender, Windows.ApplicationModel.SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            logger.LogMessage("Application suspending. Saving view models state...");
            await EpisodesModel.StoreEpisodesList();
            await PlayerModel.Playlist.SavePlaylistToLocalStorage();
            logger.LogMessage("View models state saved.");
            deferral.Complete();
        }

        #endregion                
    }
}
