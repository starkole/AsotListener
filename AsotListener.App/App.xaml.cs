namespace AsotListener.App
{
    using System;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using HockeyApp;
    using Ioc;
    using Models.Enums;
    using Services.Contracts;
    using Windows.ApplicationModel;
    using Windows.ApplicationModel.Activation;
    using Windows.Foundation.Diagnostics;
    using Windows.Storage;
    using Windows.UI.Xaml;

    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public sealed partial class App : Application
    {
        private readonly INavigationService navigationService;
        private readonly ILogger logger;
        private readonly IFileUtils fileUtils;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            IoC.Register();
            IContainer container = Container.Instance;

            InitializeComponent();
            UnhandledException += OnUnhandledException;

            navigationService = container.Resolve<INavigationService>();
            fileUtils = container.Resolve<IFileUtils>();
            logger = container.Resolve<ILogger>();
            logger.LogMessage("Application initialized.");
        }

        private async Task<bool> setupHockeyAppAsync()
        {
            string appId = null;
            try
            {
                var file = await Package.Current.InstalledLocation.GetFileAsync("hockeyapp.id").AsTask();
                appId = await FileIO.ReadTextAsync(file);
            }
            catch (Exception ex)
            {
                logger.LogMessage($"Error reading Hockey configuration. {ex.Message}", LoggingLevel.Error);
            }

            if (string.IsNullOrWhiteSpace(appId))
            {
                return false;
            }

            HockeyClient.Current.Configure(appId);
            logger.LogMessage("Hockey configured.", LoggingLevel.Information);
            return true;
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
        protected override async void OnLaunched(LaunchActivatedEventArgs args)
        {
            logger.LogMessage("Application launched.", LoggingLevel.Information);
#if DEBUG
            DebugSettings.EnableFrameRateCounter |= Debugger.IsAttached;
#endif            
            navigationService.Initialize(typeof(MainPage), NavigationParameter.OpenMainPage);
            Window.Current.Activate();
            if (await setupHockeyAppAsync())
            {
                await HockeyClient.Current.SendCrashesAsync(true);
                await HockeyClient.Current.CheckForAppUpdateAsync();
            }
        }
    }
}
