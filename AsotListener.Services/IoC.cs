namespace AsotListener.Services
{
    using Contracts;
    using Implementations;
    using Ioc;

    /// <summary>
    /// Contains methods to register current library in IoC container
    /// </summary>
    public static class IoC
    {
        private static bool isRegistered = false;

        /// <summary>
        /// Registers current library in IoC container
        /// </summary>
        public static void Register()
        {
            IContainer container = Container.Instance;

            // In some cases, especially for background tasks, Register() method
            // can be called several times - task can be rerun in the same process
            if (!isRegistered)
            {
                isRegistered = true;
                container.RegisterSingleton<ILogger, Logger>();
                container.RegisterSingleton<IParser, Parser>();
                container.RegisterSingleton<IFileUtils, FileUtils>();
                container.RegisterSingleton<ILoaderFactory, LoaderFactory>();
                container.RegisterSingleton<IApplicationSettingsHelper, ApplicationSettingsHelper>();
                container.RegisterSingleton<INavigationService, NavigationService>();
                container.RegisterSingleton<IPlaybackManager, PlaybackManager>();
                container.RegisterSingleton<IDownloadManager, DownloadManager>();
                container.RegisterSingleton<IEpisodeListManager, EpisodeListManager>();
                container.RegisterSingleton<IVoiceCommandsHandler, VoiceCommandsHandler>();
                container.RegisterSingleton<ITextSpeaker, TextSpeaker>();
            }
        }
    }
}
