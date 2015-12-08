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
    using Windows.UI.Xaml.Input;
    using Windows.UI.Input;
    using Windows.UI.Xaml.Controls.Primitives;
    using System.Diagnostics;

    public sealed partial class MainPage : Page, IDisposable
    {
        private readonly NavigationHelper navigationHelper;
        private readonly MainPageViewModel mainPageViewModel;
        private IApplicationSettingsHelper applicationSettingsHelper = ApplicationSettingsHelper.Instance;

        // TODO: Move text labels to resources
        //private readonly ResourceLoader resourceLoader = ResourceLoader.GetForCurrentView("Resources");
        // TODO: Implement logger to use LoggingSession and Debug simultaneously.
        private readonly ILogger logger;        

        public MainPage()
        {
            this.logger = Logger.Instance;

            this.mainPageViewModel = new MainPageViewModel(this.logger, this.applicationSettingsHelper);
                        
            this.NavigationCacheMode = NavigationCacheMode.Required;
            this.navigationHelper = new NavigationHelper(this);

            this.InitializeComponent();
        }

        /// <summary>
        /// Gets the <see cref="NavigationHelper"/> associated with this <see cref="Page"/>.
        /// </summary>
        public NavigationHelper NavigationHelper => this.navigationHelper;

        public MainPageViewModel MainPageViewModel => this.mainPageViewModel;

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
                    this.logger.Dispose();
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
