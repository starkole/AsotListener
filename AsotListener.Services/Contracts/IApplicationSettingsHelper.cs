namespace AsotListener.Services.Contracts
{
    public interface IApplicationSettingsHelper
    {
        T ReadSettingsValue<T>(string key);
        void SaveSettingsValue(string key, object value);
    }
}
