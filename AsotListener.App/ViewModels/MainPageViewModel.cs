namespace AsotListener.App.ViewModels
{
    using Services.Contracts;
    using Windows.UI.Xaml;
    using Models;
    using System;

    public class MainPageViewModel : BaseModel, IDisposable
    {
        #region Fields

        private readonly PlayerViewModel playerModel;
        private readonly EpisodesViewModel episodesViewModel;

        #endregion

        #region Properties

        public PlayerViewModel PlayerModel => playerModel;
        public EpisodesViewModel EpisodesModel => episodesViewModel;
        

        #endregion

        #region Ctor

        public MainPageViewModel(
            ILogger logger,
            IApplicationSettingsHelper applicationSettingsHelper,
            IFileUtils fileUtils,
            IPlayList playlist,
            IParser parser,
            ILoaderFactory loaderFactory)
        {
            playerModel = new PlayerViewModel(logger, playlist, applicationSettingsHelper);
            episodesViewModel = new EpisodesViewModel(logger, fileUtils, playlist, loaderFactory, parser);

            Application.Current.Suspending += onAppSuspending;
            Application.Current.Resuming += onAppResuming;
        }

        #endregion

        #region State Management

        private async void onAppResuming(object sender, object eventArgs)
        {
            await EpisodesModel.RestoreEpisodesList();
            await PlayerModel.Playlist.SavePlaylistToLocalStorage();
        }

        private async void onAppSuspending(object sender, Windows.ApplicationModel.SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            await EpisodesModel.StoreEpisodesList();
            await PlayerModel.Playlist.LoadPlaylistFromLocalStorage();
            deferral.Complete();
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
                    episodesViewModel.Dispose();
                    playerModel.Dispose();
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
