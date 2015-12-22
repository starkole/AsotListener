namespace AsotListener.App
{
    using ViewModels;
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

            Services.IoC.Register();

            container.RegisterType<PlayerViewModel>();
            container.RegisterType<EpisodesViewModel>();
            container.RegisterType<MainPageViewModel>();
        }
    }
}
