namespace AsotListener.App
{
    using ViewModels;
    using Ioc;

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
