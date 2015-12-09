namespace AsotListener.Services.Contracts
{
    public interface IApplicationSettingsHelper
    {
        object ReadSettingsValue(string key);
        object ReadAndRemoveSettingsValue(string key);
        void SaveSettingsValue(string key, object value);
    }
}
