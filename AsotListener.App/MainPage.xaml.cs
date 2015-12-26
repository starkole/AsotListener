namespace AsotListener.App
{
    using Ioc;
    using Models;
    using Models.Enums;
    using Services.Contracts;
    using ViewModels;
    using Windows.Phone.UI.Input;
    using Windows.UI.Input;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Controls.Primitives;
    using Windows.UI.Xaml.Input;
    using Windows.UI.Xaml.Navigation;

    /// <summary>
    /// Main page view
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private readonly MainPageViewModel mainPageViewModel;
        private IApplicationSettingsHelper applicationSettingsHelper;
        private readonly ILogger logger;

        /// <summary>
        /// Main Page view model
        /// </summary>
        public MainPageViewModel MainPageViewModel => mainPageViewModel;

        /// <summary>
        /// Creates new instance of <see cref="MainPage"/>
        /// </summary>
        public MainPage()
        {
            // TODO: Think about using static properties
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
                    MainPageViewModel.PlayerModel.PlayPauseCommand.Execute(Playlist.Instance.CurrentTrack);
                    break;
            }
        }

        #endregion

        #region Event Handlers

        // TODO: Generate context menu dynamically
        private void OnEpisodeListElementHolding(object sender, HoldingRoutedEventArgs args)
        {
            logger.LogMessage($"MainPage: Holding event with HoldingState={args.HoldingState}");

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
            logger.LogMessage("MainPage: Hardware back button pressed.");
            if (Frame.CanGoBack)
            {
                logger.LogMessage("MainPage: Navigating back.");

                // Notify system, that event was handled and it shouldn't close our application
                e.Handled = true;
                Frame.GoBack();
            }
        }

        private void onPageLoaded(object sender, RoutedEventArgs e)
        {
            logger.LogMessage("MainPage: onPageLoaded event fired.");
            HardwareButtons.BackPressed += onHardwareBackButtonPressed;
        }

        private void onPageUnloaded(object sender, RoutedEventArgs e)
        {
            logger.LogMessage("MainPage: onPageUnloaded event fired.");
            HardwareButtons.BackPressed -= onHardwareBackButtonPressed;
        }

        private void onAudioSeekSliderPointerEntered(object sender, PointerRoutedEventArgs e)
        {
            logger.LogMessage("MainPage: onAudioSeekSliderPointerEntered event fired.");
            MainPageViewModel.PlayerModel.CanUpdateAudioSeeker = false;
        }

        private void onAudioSeekSliderPointerCaptureLost(object sender, PointerRoutedEventArgs e)
        {
            logger.LogMessage("MainPage: onAudioSeekSliderPointerCaptureLost event fired.");
            MainPageViewModel.PlayerModel.UpdateProgressFromSlider(sender as Slider);
            MainPageViewModel.PlayerModel.CanUpdateAudioSeeker = true;
        }

        #endregion
    }
}
