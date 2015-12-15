namespace AsotListener.Services.Contracts
{
    using System;
    using Models.Enums;

    public interface INavigationService
    {
        void Navigate(NavigationParameter parameter);
        void Initialize(Type mainPageType, NavigationParameter parameter);
    }
}