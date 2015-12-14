namespace AsotListener.Services
{
    using Contracts;
    using Implementations;
    using Ioc;

    public static class IoC
    {
        public static void Register()
        {
            IContainer container = Container.Instance;

            container.RegisterSingleton<ILogger, Logger>();
            container.RegisterSingleton<IParser, Parser>();
            container.RegisterSingleton<IFileUtils, FileUtils>();
            container.RegisterSingleton<IPlayList, Playlist>();
            container.RegisterSingleton<ILoaderFactory, LoaderFactory>();
            container.RegisterSingleton<IApplicationSettingsHelper, ApplicationSettingsHelper>();
            container.RegisterSingleton<INavigationService, NavigationService>();
        }
    }
}
