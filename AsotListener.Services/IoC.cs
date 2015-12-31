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
        /// <summary>
        /// Registers current library in IoC container
        /// </summary>
        public static void Register()
        {
            IContainer container = Container.Instance;

            container.RegisterSingleton<ILogger, Logger>();
            container.RegisterSingleton<IParser, Parser>();
            container.RegisterSingleton<IFileUtils, FileUtils>();
            container.RegisterSingleton<ILoaderFactory, LoaderFactory>();
            container.RegisterSingleton<IApplicationSettingsHelper, ApplicationSettingsHelper>();
            container.RegisterSingleton<INavigationService, NavigationService>();
            container.RegisterSingleton<IPlaybackManager, PlaybackManager>();
        }
    }
}
