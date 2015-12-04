namespace AsotListener.App
{
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Navigation;
    using Services.Navigation;
    using ViewModels;
    using Windows.Foundation.Diagnostics;
    using System;
    using Models;
    using Services;

    public sealed partial class MainPage : Page, IDisposable
    {
        private readonly NavigationHelper navigationHelper;
        private readonly MainPageViewModel mainPageViewModel;
        private readonly PlayerViewModel playerModel;        
        private IApplicationSettingsHelper applicationSettingsHelper = ApplicationSettingsHelper.Instance;

        // TODO: Move text labels to resources
        //private readonly ResourceLoader resourceLoader = ResourceLoader.GetForCurrentView("Resources");
        // TODO: Implement logger to use LoggingSession and Debug simultaneously.
        private readonly ILoggingSession loggingSession;        

        public MainPage()
        {
            if (!Application.Current.Resources.ContainsKey(Constants.LOGGING_SESSION_NAME))
            {
                Application.Current.Resources[Constants.LOGGING_SESSION_NAME] = new LoggingSession(Constants.LOGGING_SESSION_NAME);
            }
            this.loggingSession = (LoggingSession)Application.Current.Resources[Constants.LOGGING_SESSION_NAME];

            this.mainPageViewModel = new MainPageViewModel(this.loggingSession, this.applicationSettingsHelper);
            this.playerModel = new PlayerViewModel();

            this.InitializeComponent();

            this.NavigationCacheMode = NavigationCacheMode.Required;
            this.navigationHelper = new NavigationHelper(this);
        }

        /// <summary>
        /// Gets the <see cref="NavigationHelper"/> associated with this <see cref="Page"/>.
        /// </summary>
        public NavigationHelper NavigationHelper => this.navigationHelper;

        public MainPageViewModel MainPageViewModel => this.mainPageViewModel;
        public PlayerViewModel PlayerModel => this.playerModel;

        #region NavigationHelper registration

        /// <summary>
        /// The methods provided in this section are simply used to allow
        /// NavigationHelper to respond to the page's navigation methods.
        /// <para>
        /// Page specific logic should be placed in event handlers for the  
        /// <see cref="NavigationHelper.LoadState"/>
        /// and <see cref="NavigationHelper.SaveState"/>.
        /// The navigation parameter is available in the LoadState method 
        /// in addition to page state preserved during an earlier session.
        /// </para>
        /// </summary>
        /// <param name="e">Provides data for navigation methods and event
        /// handlers that cannot cancel the navigation request.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedTo(e);
            applicationSettingsHelper.SaveSettingsValue(Constants.AppState, Constants.ForegroundAppActive);
        }
        
        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedFrom(e);
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Application.Current.Resources.Remove(Constants.LOGGING_SESSION_NAME);
                    this.loggingSession.Dispose();
                    this.playerModel.Dispose();
                    this.mainPageViewModel.Dispose();
                }

                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            Dispose(true);
        }
        #endregion

        #endregion

        private void ListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            // TODO: Find out how to move this handler to view model
            if (e.ClickedItem != null)
            {
                this.MainPageViewModel.SelectedEpisode = (Episode)e.ClickedItem;
            }
        }
    }
}
