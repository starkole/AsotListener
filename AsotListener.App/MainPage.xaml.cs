namespace AsotListener.App
{
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Navigation;
    using Services.Navigation;
    using ViewModels;
    using System;
    using Models;
    using Windows.UI.Xaml.Input;
    using Windows.UI.Input;
    using Windows.UI.Xaml.Controls.Primitives;
    using System.Diagnostics;
    using Windows.Media.Playback;
    using Services.Contracts;
    using Services.Implementations;

    public sealed partial class MainPage : Page, IDisposable
    {
        private readonly NavigationHelper navigationHelper;
        private readonly MainPageViewModel mainPageViewModel;
        private IApplicationSettingsHelper applicationSettingsHelper;
        private readonly ILogger logger;

        // TODO: Move text labels to resources
        //private readonly ResourceLoader resourceLoader = ResourceLoader.GetForCurrentView("Resources");

        public MainPage()
        {
            logger = Logger.Instance;
            applicationSettingsHelper = ApplicationSettingsHelper.Instance;
            var fileUtils = FileUtils.Instance;
            var playlist = Playlist.Instance;
            var loaderFactory = new LoaderFactory(logger, fileUtils);
            var parser = Parser.Instance;

            mainPageViewModel = new MainPageViewModel(logger, applicationSettingsHelper, fileUtils, playlist, parser, loaderFactory);
                   
            NavigationCacheMode = NavigationCacheMode.Required;
            navigationHelper = new NavigationHelper(this);

            InitializeComponent();

            logger.LogMessage("MainPage has been created.");
        }

        /// <summary>
        /// Gets the <see cref="NavigationHelper"/> associated with this <see cref="Page"/>.
        /// </summary>
        public NavigationHelper NavigationHelper => navigationHelper;

        public MainPageViewModel MainPageViewModel => mainPageViewModel;

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
            logger.LogMessage("Navigated to MainPage.");
            this.navigationHelper.OnNavigatedTo(e);
            applicationSettingsHelper.SaveSettingsValue(Constants.AppState, Constants.ForegroundAppActive);
            var param = e.Parameter as string;
            if (param == Constants.StartPlayback)
            {
                logger.LogMessage("Starting playback from MainPage navigation handler.");
                MainPivot.SelectedItem = PlayerPivotItem;
                BackgroundMediaPlayer.Current.Pause();
                MainPageViewModel.PlayerModel.PlayPauseCommand.Execute(MainPageViewModel.PlayerModel.Playlist.CurrentTrack);
            }
        }
        
        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            logger.LogMessage("Navigated away from MainPage.");
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
                    mainPageViewModel.Dispose();
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

        private void OnElementHolding(object sender, HoldingRoutedEventArgs args)
        {
            // TODO: Find out how to move this handler to view model
            Debug.WriteLine("Holding event with HoldingState=" + args.HoldingState.ToString());

            // this event is fired multiple times. We do not want to show the menu twice
            if (args.HoldingState != HoldingState.Started) return;

            FrameworkElement element = sender as FrameworkElement;
            if (element == null) return;

            // If the menu was attached properly, we just need to call this handy method
            FlyoutBase.ShowAttachedFlyout(element);
        }
    }
}
