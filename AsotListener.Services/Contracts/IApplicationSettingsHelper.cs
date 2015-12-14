namespace AsotListener.Services.Contracts
{
    public interface IApplicationSettingsHelper
    {
        T ReadSettingsValue<T>(string key) where T: class;
        void SaveSettingsValue(string key, object value);
    }
}
