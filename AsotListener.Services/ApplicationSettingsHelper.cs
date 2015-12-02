namespace AsotListener.Services
{
    using System;
    using Windows.Storage;

    public sealed class ApplicationSettingsHelper : IApplicationSettingsHelper
    {
        private static Lazy<IApplicationSettingsHelper> lazy =
            new Lazy<IApplicationSettingsHelper>(() => new ApplicationSettingsHelper());

        public static IApplicationSettingsHelper Instance { get { return lazy.Value; } }

        private ApplicationSettingsHelper() { }

        /// <summary>
        /// Function to read a setting value and clear it after reading it
        /// </summary>
        public object ReadSettingsValue(string key)
        {
            // TODO: Add logging here
            if (!ApplicationData.Current.LocalSettings.Values.ContainsKey(key))
            {
                return null;
            }
            else
            {
                var value = ApplicationData.Current.LocalSettings.Values[key];
                return value;
            }
        }

        /// <summary>
        /// Function to read a setting value and clear it after reading it
        /// </summary>
        public object ReadAndRemoveSettingsValue(string key)
        {
            // TODO: Add logging here
            if (!ApplicationData.Current.LocalSettings.Values.ContainsKey(key))
            {
                return null;
            }
            else
            {
                var value = ApplicationData.Current.LocalSettings.Values[key];
                ApplicationData.Current.LocalSettings.Values.Remove(key);
                return value;
            }
        }

        /// <summary>
        /// Save a key value pair in settings. Create if it doesn't exist
        /// </summary>
        public void SaveSettingsValue(string key, object value)
        {
            // TODO: Add logging here
            if (!ApplicationData.Current.LocalSettings.Values.ContainsKey(key))
            {
                ApplicationData.Current.LocalSettings.Values.Add(key, value);
            }
            else
            {
                ApplicationData.Current.LocalSettings.Values[key] = value;
            }
        }
    }
}
