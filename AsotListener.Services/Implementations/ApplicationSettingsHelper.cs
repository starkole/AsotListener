namespace AsotListener.Services.Implementations
{
    using System;
    using System.Threading;
    using Contracts;
    using Windows.Foundation.Diagnostics;
    using Windows.Storage;

    /// <summary>
    /// Helper class to read and write settings to LocalSettings
    /// </summary>
    public sealed class ApplicationSettingsHelper : IApplicationSettingsHelper
    {
        private readonly ILogger logger;
        private const string mutexName = "AsotListener.ApplicationSettingsHelper.Mutex";

        /// <summary>
        /// Creates an instance of <see cref="ApplicationSettingsHelper"/> class
        /// </summary>
        /// <param name="logger">The logger instance</param>
        public ApplicationSettingsHelper(ILogger logger)
        {
            this.logger = logger;
            logger.LogMessage("ApplicationSettingsHelper initialized.", LoggingLevel.Information);
        }

        /// <summary>
        /// Function to read a setting value and clear it after reading it
        /// </summary>
        /// <typeparam name="T">The value type</typeparam>
        /// <param name="key">The value key</param>
        /// <returns>The value, casted to type <see cref="T"/>, which corresponds to given key</returns>
        public T ReadSettingsValue<T>(string key)
        {
            using (var mutex = new Mutex(true, mutexName))
            {
                mutex.WaitOne();
                try
                {
                    logger.LogMessage($"Reading {key} parameter from LoaclSettings.");
                    if (!ApplicationData.Current.LocalSettings.Values.ContainsKey(key))
                    {
                        logger.LogMessage($"No {key} parameter found in LoaclSettings.", LoggingLevel.Warning);
                        return default(T);
                    }
                    else
                    {
                        var value = (T)ApplicationData.Current.LocalSettings.Values[key];
                        return value;
                    }
                }
                catch (Exception ex)
                {
                    logger.LogMessage($"Exception on reading from LoaclSettings. {ex.Message}", LoggingLevel.Error);
                    return default(T);
                }
                finally
                {
                    mutex.ReleaseMutex();
                }
            }
        }

        /// <summary>
        /// Save a key-value pair in settings. Create if it doesn't exist
        /// </summary>
        public void SaveSettingsValue(string key, object value)
        {
            using (var mutex = new Mutex(true, mutexName))
            {
                mutex.WaitOne();
                try
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
                catch (Exception ex)
                {
                    logger.LogMessage($"Exception on saving to LoaclSettings. {ex.Message}", LoggingLevel.Error);
                }
                finally
                {
                    mutex.ReleaseMutex();
                }
            }
        }
    }
}
