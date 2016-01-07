namespace AsotListener.Services.Contracts
{
    using System.Threading.Tasks;
    using Common;

    public interface IApplicationSettingsHelper: IAsyncInitialization
    {
        T ReadSettingsValue<T>(string key);
        void SaveSettingsValue(string key, object value);
        Task SavePlaylist();
        Task SavePlaylistWithNotification();
        Task LoadPlaylist();
        Task SaveEpisodeList();
        Task LoadEpisodeList();
    }
}
