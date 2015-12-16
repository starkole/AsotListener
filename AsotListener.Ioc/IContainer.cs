namespace AsotListener.Ioc
{
    public interface IContainer
    {
        void RegisterSingleton<TInterface, TImplementation>() where TImplementation : TInterface;
        void RegisterType<TInterface, TImplementation>() where TImplementation : TInterface;
        void RegisterType<T>();
        void RegisterInstance<T>(T instance);
        TInterface Resolve<TInterface>();
    }
}
