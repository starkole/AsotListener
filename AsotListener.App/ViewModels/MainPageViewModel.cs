namespace AsotListener.App.ViewModels
{
    using Services;
    using Models;
    using System;

    public class MainPageViewModel : BaseModel, IDisposable
    {
        #region Fields

        private readonly IApplicationSettingsHelper applicationSettingsHelper;
        private readonly PlayerViewModel playerModel;
        private readonly EpisodesViewModel episodesViewModel;

        #endregion

        #region Properties

        public PlayerViewModel PlayerModel => this.playerModel;
        public EpisodesViewModel EpisodesModel => this.episodesViewModel;
        

        #endregion

        #region Ctor

        public MainPageViewModel(
            ILogger logger,
            IApplicationSettingsHelper applicationSettingsHelper,
            IFileUtils fileUtils,
            IPlayList playlist)
        {
            this.playerModel = new PlayerViewModel(logger);
            this.episodesViewModel = new EpisodesViewModel(logger, fileUtils, playlist);

            this.applicationSettingsHelper = applicationSettingsHelper;
            

            App.Current.Suspending += onAppSuspending;
            App.Current.Resuming += onAppResuming;
        }

        #endregion

        #region State Management

        private async void onAppResuming(object sender, object eventArgs)
        {
            await EpisodesModel.RestoreEpisodesList();            
        }

        private async void onAppSuspending(object sender, Windows.ApplicationModel.SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            await EpisodesModel.StoreEpisodesList();
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
                    this.episodesViewModel.Dispose();
                    this.playerModel.Dispose();
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
