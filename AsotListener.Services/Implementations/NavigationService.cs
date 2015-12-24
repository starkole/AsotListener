namespace AsotListener.Services.Implementations
{
    using System;
    using Models.Enums;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Media.Animation;
    using Windows.UI.Xaml.Navigation;
    using Contracts;
    using Windows.Foundation.Diagnostics;
    using Windows.Globalization;

    /// <summary>
    /// Provides helper methods to navigate between application pages
    /// </summary>
    public class NavigationService : INavigationService
    {
        #region Fileds

        private Frame frame;
        private Type mainPageType;
        private TransitionCollection transitions;
        private readonly ILogger logger;
        private bool isInitialized = false;
        private EventHandler<object> deferredNavigationHandler;

        #endregion

        //TODO: Move to App project.

        #region Ctor

        /// <summary>
        /// Creates new instance of <see cref="NavigationService"/>
        /// </summary>
        /// <param name="logger">The logger instance</param>
        public NavigationService(ILogger logger)
        {
            this.logger = logger;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Initializes <see cref="NavigationService"/> instance
        /// </summary>
        /// <param name="mainPageType">The type of the main application page</param>
        /// <param name="parameter">Initial navigation parameter</param>
        public void Initialize(Type mainPageType, NavigationParameter parameter)
        {
            this.mainPageType = mainPageType;
            Frame frame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active.
            if (frame == null)
            {
                logger.LogMessage("NavigationService: No root frame found. Creating the new one.");
                frame = new Frame
                {
                    CacheSize = 1,
                    Language = ApplicationLanguages.Languages[0]
                };
                Window.Current.Content = frame;
                logger.LogMessage("NavigationService: Root frame created successfully.");
            }

            if (frame.Content == null)
            {
                logger.LogMessage("NavigationService: Root frame is empty. Removing transitions for the first-time navigation.");
                if (frame.ContentTransitions != null)
                {
                    transitions = new TransitionCollection();
                    foreach (var c in frame.ContentTransitions)
                    {
                        transitions.Add(c);
                    }
                }

                frame.ContentTransitions = null;
                frame.Navigated += onRootFrameFirstNavigated;
            }

            this.frame = frame;
            if (!frame.Navigate(mainPageType, parameter))
            {
                string message = "Navigation service initialization error.";
                logger.LogMessage(message, LoggingLevel.Critical);
                throw new Exception(message);
            }

            logger.LogMessage("NavigationService initialized.", LoggingLevel.Information);
        }

        /// <summary>
        /// Navigates to the main application page with passing given parameter to it
        /// </summary>
        /// <param name="parameter">Navigation parameter to pass</param>
        public void Navigate(NavigationParameter parameter)
        {
            if (!isInitialized)
            {
                logger.LogMessage($"NavigationService: Root frame hasn't initialized yet. Schedule navigation with parameter {parameter}.");
                deferredNavigationHandler = (_, __) =>
                {
                    logger.LogMessage("NavigationService: Executing deferred navigation handler.");
                    frame.LayoutUpdated -= deferredNavigationHandler;
                    Navigate(parameter);
                };
                return;
            }

            logger.LogMessage($"NavigationService: Navigating with parameter {parameter}.");

            if (!frame.Navigate(mainPageType, parameter))
            {
                logger.LogMessage("Navigation error.", LoggingLevel.Error);
            }
        }

        #endregion

        #region Private Methods

        private void onRootFrameFirstNavigated(object sender, NavigationEventArgs e)
        {
            logger.LogMessage("NavigationService: Navigated to root frame for the first time.");
            var rootFrame = sender as Frame;
            rootFrame.ContentTransitions = transitions ?? new TransitionCollection { new NavigationThemeTransition() };
            rootFrame.Navigated -= onRootFrameFirstNavigated;
            logger.LogMessage("NavigationService: Root frame transitions restored.");
            isInitialized = true;
            if (deferredNavigationHandler != null)
            {
                rootFrame.LayoutUpdated += deferredNavigationHandler;
            }
        }

        #endregion
    }
}
