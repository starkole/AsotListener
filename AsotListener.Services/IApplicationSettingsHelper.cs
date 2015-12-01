namespace AsotListener.Services
{
    public interface IApplicationSettingsHelper
    {
        object ReadSettingsValue(string key);
        void SaveSettingsValue(string key, object value);
    }
}
