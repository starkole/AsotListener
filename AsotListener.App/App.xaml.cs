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
    using Windows.UI.Xaml;

    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public sealed partial class App : Application
    {
        #region Declarations

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
            logger.LogMessage("Application launched.", LoggingLevel.Information);
#if DEBUG
            try
            {
                // Prevent display to turn off while debugging.
                var displayRequest = new Windows.System.Display.DisplayRequest();
                displayRequest.RequestActive();
                Suspending += (_, __) => displayRequest.RequestRelease();
                UnhandledException += (_, __) => displayRequest.RequestRelease();
            }
            catch (Exception ex)
            {
                logger.LogMessage($"Error setting up display request for debugging. {ex.Message}", LoggingLevel.Error);
            }
            DebugSettings.BindingFailed += (o, a) => logger.LogMessage($"BindingFailed. {a.Message}", LoggingLevel.Error);
            DebugSettings.EnableFrameRateCounter = Debugger.IsAttached;
#endif            
            var navigationService = container.Resolve<INavigationService>();
            navigationService.Initialize(typeof(MainPage), NavigationParameter.OpenMainPage);
            Window.Current.Activate();
            await setupVoiceCommandsAsync();
            await RegisterBackgroundUpdaterTask();
            if (await setupHockeyAppAsync())
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

        #endregion

        #region Helpers

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
