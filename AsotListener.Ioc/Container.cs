namespace AsotListener.Ioc
{
    using System;
    using Microsoft.Practices.Unity;

    public class Container: IContainer
    {
        private static UnityContainer container = new UnityContainer();
        private static Lazy<IContainer> lazy = new Lazy<IContainer>(() => new Container());

        public static IContainer Instance => lazy.Value;

        private Container() { }

        public void RegisterSingleton<TInterface, TImplementation>() where TImplementation: TInterface
        {
            container.RegisterType<TInterface, TImplementation>(new ContainerControlledLifetimeManager());
        }

        public void RegisterType<TInterface, TImplementation>() where TImplementation : TInterface
        {
            container.RegisterType<TInterface, TImplementation>(new TransientLifetimeManager());
        }

        public void RegisterType<T>()
        {
            container.RegisterType<T>(new TransientLifetimeManager());            
        }

        public void RegisterInstance<T>(T instance)
        {
            container.RegisterInstance(instance, new ContainerControlledLifetimeManager());
        }

        public TInterface Resolve<TInterface>() => container.Resolve<TInterface>();
    }
}
