namespace AsotListener.App
{
    using ViewModels;
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
            if (!isRegistered)
            {
                isRegistered = true;
                Services.IoC.Register();
                IContainer container = Container.Instance;

                // TODO: Define interfaces for view models
                container.RegisterType<PlayerViewModel>();
                container.RegisterType<EpisodesViewModel>();
                container.RegisterType<MainPageViewModel>();
            }
        }
    }
}
