namespace AsotListener.App
{
    using ViewModels;
    using Ioc;
    using Windows.Media.Playback;

    public static class IoC
    {
        public static void Register()
        {
            IContainer container = Container.Instance;

            Services.IoC.Register();

            container.RegisterType<PlayerViewModel>();
            container.RegisterType<EpisodesViewModel>();
            container.RegisterType<MainPageViewModel>();
        }
    }
}
