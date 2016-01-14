namespace AsotListener.App
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;
    using Common;
    using HockeyApp;
    using Ioc;
    using Models.Enums;
    using Services.Contracts;
    using Windows.ApplicationModel;
    using Windows.ApplicationModel.Activation;
    using Windows.ApplicationModel.Background;
    using Windows.Foundation.Diagnostics;
    using Windows.Media.SpeechRecognition;
    using Windows.Storage;
    using Windows.UI.Core;
    using Windows.UI.Xaml;

    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public sealed partial class App : Application
    {
        #region Declarations

        private static bool isHockeyConfigured = false;
        private readonly ILogger logger;
        private IContainer container => Container.Instance;

        #endregion

        #region Ctor

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            IoC.Register();
            InitializeComponent();
            UnhandledException -= onUnhandledException;
            UnhandledException += onUnhandledException;
            logger = container.Resolve<ILogger>();
            logger.LogMessage("Application initialized.");            
        }        

        #endregion

        #region Overrides

        /// <summary>
        /// Invoked when application is activated by system, for example via voice command
        /// </summary>
        /// <param name="args">Activation arguments</param>
        protected override async void OnActivated(IActivatedEventArgs args)
        {
            base.OnActivated(args);

            logger.LogMessage("Application activated.", LoggingLevel.Information);
            if (args.Kind == ActivationKind.VoiceCommand)
            {
                var voiceCommandsHandler = container.Resolve<IVoiceCommandsHandler>();
                await voiceCommandsHandler.Initialization;
                await voiceCommandsHandler.HandleVoiceCommnadAsync(args as VoiceCommandActivatedEventArgs);
            }

            if (args.PreviousExecutionState == ApplicationExecutionState.Running ||
                args.PreviousExecutionState == ApplicationExecutionState.Suspended)
            {
                return;
            }

            Exit();
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override async void OnLaunched(LaunchActivatedEventArgs args)
        {
            base.OnLaunched(args);

            var coreWindow = CoreWindow.GetForCurrentThread();
            coreWindow.Activated -= onCoreWindowAppActivated;
            coreWindow.Activated += onCoreWindowAppActivated;
            coreWindow.Closed -= onCoreWindowAppClosed;
            coreWindow.Closed += onCoreWindowAppClosed;

            logger.LogMessage("Application launched.", LoggingLevel.Information);
#if DEBUG            
            DebugSettings.BindingFailed += (o, a) => logger.LogMessage($"BindingFailed. {a.Message}", LoggingLevel.Error);
            DebugSettings.EnableFrameRateCounter = Debugger.IsAttached;
#endif            
            var navigationService = container.Resolve<INavigationService>();
            navigationService.Initialize(typeof(MainPage), NavigationParameter.OpenMainPage);
            Window.Current.Activate();
            await setupVoiceCommandsAsync();
            await RegisterBackgroundUpdaterTask();
            await setupHockeyAppAsync();
            if (isHockeyConfigured)
            {
                await HockeyClient.Current.SendCrashesAsync(true);
                await HockeyClient.Current.CheckForAppUpdateAsync();
            }
        }

        #endregion

        #region Handlers

        private void onUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            logger.LogMessage($"Unhandled exception occurred. {e.Message}", LoggingLevel.Critical);
            logger.SaveLogsToFile();
        }

        private void onCoreWindowAppClosed(CoreWindow sender, CoreWindowEventArgs args)
        {
            logger.LogMessage($"Core Window closed");
        }

        private void onCoreWindowAppActivated(CoreWindow sender, WindowActivatedEventArgs args)
        {
            logger.LogMessage($"Core window activated with state {args.WindowActivationState}");
        }

        #endregion

        #region Helpers

        private async Task setupHockeyAppAsync()
        {
            if (isHockeyConfigured)
            {
                return;
            }

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
                return;
            }

            HockeyClient.Current.Configure(appId);
            logger.LogMessage("Hockey configured.", LoggingLevel.Information);
            isHockeyConfigured = true;
        }

        private async Task setupVoiceCommandsAsync()
        {
            try
            {
                var storageFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///AsotListenerCommands.xml"));
                await VoiceCommandManager.InstallCommandSetsFromStorageFileAsync(storageFile);
            }
            catch (Exception ex)
            {
                logger.LogMessage($"Error installing voice commands set. {ex.Message}", LoggingLevel.Error);
            }
            logger.LogMessage("Voice commands installed.", LoggingLevel.Information);
        }

        private async Task RegisterBackgroundUpdaterTask()
        {
            const int taskIntervalHours = 25;

            if (BackgroundTaskRegistration.AllTasks.Any(t => t.Value.Name == Constants.BackgroundUpdaterTaskName))
            {
                // Task already registered
                return;
            }

            var accessStatus = await BackgroundExecutionManager.RequestAccessAsync();
            if (accessStatus != BackgroundAccessStatus.AllowedMayUseActiveRealTimeConnectivity)
            {
                logger.LogMessage($"Attempted to register {Constants.BackgroundUpdaterTaskName} task, but system hasn't allowed it.", LoggingLevel.Warning);
                return;
            }

            var builder = new BackgroundTaskBuilder
            {
                Name = Constants.BackgroundUpdaterTaskName,
                TaskEntryPoint = Constants.BackgroundUpdaterTaskName + ".BackgroundUpdaterTask"
            };

            TimeTrigger trigger = new TimeTrigger(60 * taskIntervalHours, false);
            builder.SetTrigger(trigger);

            try
            {
                BackgroundTaskRegistration task = builder.Register();
            }
            catch (Exception ex)
            {
                logger.LogMessage($"Error registering {Constants.BackgroundUpdaterTaskName} task. {ex.Message}", LoggingLevel.Error);
            }

            logger.LogMessage($"Registered {Constants.BackgroundUpdaterTaskName} task to run every {taskIntervalHours} hours.", LoggingLevel.Information);
        }

        #endregion
    }
}
