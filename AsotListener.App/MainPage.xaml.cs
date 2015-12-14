namespace AsotListener.App
{
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Navigation;
    using ViewModels;
    using Models;
    using Windows.UI.Xaml.Input;
    using Windows.UI.Input;
    using Windows.UI.Xaml.Controls.Primitives;
    using Services.Contracts;
    using Ioc;
    using Models.Enums;
    using Windows.Phone.UI.Input;

    public sealed partial class MainPage : Page
    {
        private readonly MainPageViewModel mainPageViewModel;
        private IApplicationSettingsHelper applicationSettingsHelper;
        private readonly ILogger logger;

        public MainPageViewModel MainPageViewModel => mainPageViewModel;

        public MainPage()
        {
            IContainer container = Container.Instance;

            logger = container.Resolve<ILogger>();
            applicationSettingsHelper = container.Resolve<IApplicationSettingsHelper>();
            mainPageViewModel = container.Resolve<MainPageViewModel>();
                   
            NavigationCacheMode = NavigationCacheMode.Required;
            
            InitializeComponent();
            Loaded += onPageLoaded;
            Unloaded += onPageUnloaded;

            logger.LogMessage("MainPage has been created.");
        }

        #region Navigation

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
            base.OnNavigatedTo(e);
            logger.LogMessage($"Navigated to MainPage with parameter {e.Parameter}.");
            applicationSettingsHelper.SaveSettingsValue(Constants.AppState, ForegroundAppStatus.Active.ToString());

            //TODO: Check if this works.
            NavigationParameter navigationParameter = e.Parameter is NavigationParameter ?
                (NavigationParameter)e.Parameter : 
                NavigationParameter.OpenMainPage;

            switch (navigationParameter)
            {
                case NavigationParameter.OpenPlayer:
                    logger.LogMessage("Opening player from MainPage navigation handler.");
                    MainPivot.SelectedItem = PlayerPivotItem;
                    break;
                case NavigationParameter.StartPlayback:
                    logger.LogMessage("Starting playback from MainPage navigation handler.");
                    MainPivot.SelectedItem = PlayerPivotItem;
                    MainPageViewModel.PlayerModel.PlayPauseCommand.Execute(MainPageViewModel.PlayerModel.Playlist.CurrentTrack);
                    break;
                default:
                    break;
            }
        }
        
        #endregion
       
        #region Event Handlers

        private void OnEpisodeListElementHolding(object sender, HoldingRoutedEventArgs args)
        {
            logger.LogMessage($"Holding event with HoldingState={args.HoldingState}");

            // this event is fired multiple times. We do not want to show the menu twice
            if (args.HoldingState == HoldingState.Started)
            {
                FrameworkElement element = sender as FrameworkElement;
                if (element != null)
                {
                    // If the menu was attached properly, we just need to call this handy method
                    logger.LogMessage("MainPage: Opening context menu.");
                    FlyoutBase.ShowAttachedFlyout(element);
                }
            }
        }

        private void onHardwareBackButtonPressed(object sender, BackPressedEventArgs e)
        {
            logger.LogMessage("Hardware back button pressed.");
            if (Frame.CanGoBack)
            {
                logger.LogMessage("Navigating back.");
                e.Handled = true;
                Frame.GoBack();
            }
        }

        private void onPageLoaded(object sender, RoutedEventArgs e)
        {
            HardwareButtons.BackPressed += onHardwareBackButtonPressed;
        }

        private void onPageUnloaded(object sender, RoutedEventArgs e)
        {
            HardwareButtons.BackPressed -= onHardwareBackButtonPressed;
        }

        #endregion
    }
}
