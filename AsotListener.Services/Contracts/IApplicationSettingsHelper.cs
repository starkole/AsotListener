namespace AsotListener.Services.Contracts
{
    using System.Threading.Tasks;
    using AsotListener.Models;

    public interface IApplicationSettingsHelper
    {
        T ReadSettingsValue<T>(string key);
        void SaveSettingsValue(string key, object value);
        Task SavePlaylist();
        Task LoadPlaylist();
    }
}
