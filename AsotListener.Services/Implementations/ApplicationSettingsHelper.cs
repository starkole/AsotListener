﻿namespace AsotListener.Services.Implementations
{
    using Contracts;
    using Windows.Foundation.Diagnostics;
    using Windows.Storage;

    public sealed class ApplicationSettingsHelper : IApplicationSettingsHelper
    {
        private ILogger logger;

        public ApplicationSettingsHelper(ILogger logger)
        {
            this.logger = logger;
        }

        /// <summary>
        /// Function to read a setting value and clear it after reading it
        /// </summary>
        public T ReadSettingsValue<T>(string key) where T : class
        {
            logger.LogMessage($"Reading {key} parameter from LoaclSettings.");
            if (!ApplicationData.Current.LocalSettings.Values.ContainsKey(key))
            {
                logger.LogMessage($"No {key} parameter found in LoaclSettings.", LoggingLevel.Warning);
                return null;
            }
            else
            {
                var value = ApplicationData.Current.LocalSettings.Values[key] as T;
                if (value == null)
                {
                    logger.LogMessage($"Cannot cast {key} parameter to type {typeof(T)}.", LoggingLevel.Warning);
                }

                return value;
            }
        }

        /// <summary>
        /// Save a key value pair in settings. Create if it doesn't exist
        /// </summary>
        public void SaveSettingsValue(string key, object value)
        {
            logger.LogMessage($"Saving {key} parameter to LoaclSettings.");
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
