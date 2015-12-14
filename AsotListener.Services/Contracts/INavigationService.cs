namespace AsotListener.Services.Contracts
{
    using System;
    using Models.Enums;

    public interface INavigationService
    {
        Type MainPageType { get; set; } // TODO: Think about moving this to ctor

        void Navigate(NavigationParameter parameter);
        void Initialize(NavigationParameter parameter); // TODO: Think about moving this to ctor
    }
}