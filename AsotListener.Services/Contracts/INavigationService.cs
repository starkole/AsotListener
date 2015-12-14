namespace AsotListener.Services.Contracts
{
    using System;
    using Models.Enums;

    public interface INavigationService
    {
        Type MainPageType { get; set; }

        void Navigate(NavigationParameter parameter);
    }
}