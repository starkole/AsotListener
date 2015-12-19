namespace AsotListener.App
{
    using Windows.ApplicationModel.Activation;
    using Windows.UI.Xaml;
    using System.Diagnostics;
    using Services.Contracts;
    using Ioc;
    using Windows.Foundation.Diagnostics;
    using Models.Enums;

    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public sealed partial class App : Application
    {
        private INavigationService navigationService;
        private ILogger logger;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            AsotListener.App.IoC.Register();
            IContainer container = Container.Instance;

            InitializeComponent();
            UnhandledException += OnUnhandledException;

            navigationService = container.Resolve<INavigationService>();
            logger = container.Resolve<ILogger>();
            logger.LogMessage("Application initialized.");
        }

        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            logger.LogMessage($"Unhandled exception occurred. {e.Message}", LoggingLevel.Critical);
            logger.SaveLogsToFile();
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            logger.LogMessage("Application launched.");
#if DEBUG
            DebugSettings.EnableFrameRateCounter |= Debugger.IsAttached;
#endif            
            navigationService.Initialize(typeof(MainPage), NavigationParameter.OpenMainPage);
            Window.Current.Activate();
        }
    }
}
