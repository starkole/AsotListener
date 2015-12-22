namespace AsotListener.Ioc
{
    using System;
    using Microsoft.Practices.Unity;

    /// <summary>
    /// Wrapper for Microsoft Unity IoC container
    /// </summary>
    public class Container: IContainer
    {
        private static readonly UnityContainer container = new UnityContainer();
        private static Lazy<IContainer> lazy = new Lazy<IContainer>(() => new Container());

        /// <summary>
        /// Returns container instance
        /// </summary>
        public static IContainer Instance => lazy.Value;

        private Container() { }

        /// <summary>
        /// Registers type as singleton
        /// </summary>
        /// <typeparam name="TInterface">Interface type</typeparam>
        /// <typeparam name="TImplementation">Type, that implements interfase</typeparam>
        public void RegisterSingleton<TInterface, TImplementation>() where TImplementation: TInterface
        {
            container.RegisterType<TInterface, TImplementation>(new ContainerControlledLifetimeManager());
        }

        /// <summary>
        /// Registers type, so that container creates new type instance on every call to resolver
        /// </summary>
        /// <typeparam name="TInterface">Interface type</typeparam>
        /// <typeparam name="TImplementation">Type, that implements interfase</typeparam>
        public void RegisterType<TInterface, TImplementation>() where TImplementation : TInterface
        {
            container.RegisterType<TInterface, TImplementation>(new TransientLifetimeManager());
        }

        /// <summary>
        /// Regosters type
        /// </summary>
        /// <typeparam name="T">Type to register</typeparam>
        public void RegisterType<T>()
        {
            container.RegisterType<T>(new TransientLifetimeManager());            
        }

        /// <summary>
        /// Registers instance
        /// </summary>
        /// <typeparam name="T">Instance type</typeparam>
        /// <param name="instance">Instance object</param>
        public void RegisterInstance<T>(T instance)
        {
            container.RegisterInstance(instance, new ContainerControlledLifetimeManager());
        }

        /// <summary>
        /// Returns instance of given type
        /// </summary>
        /// <typeparam name="TInterface">Instance type</typeparam>
        /// <returns>Instance of given type</returns>
        public TInterface Resolve<TInterface>() => container.Resolve<TInterface>();
    }
}
