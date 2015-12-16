﻿namespace AsotListener.Services.Implementations
{
    using System;
    using System.Threading;
    using Contracts;
    using Windows.Foundation.Diagnostics;
    using Windows.Storage;

    public sealed class ApplicationSettingsHelper : IApplicationSettingsHelper
    {
        private ILogger logger;
        private const string mutexName = "AsotListener.ApplicationSettingsHelper.Mutex";

        public ApplicationSettingsHelper(ILogger logger)
        {
            this.logger = logger;
            logger.LogMessage("ApplicationSettingsHelper initialized.");
        }

        /// <summary>
        /// Function to read a setting value and clear it after reading it
        /// </summary>
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
        /// Save a key value pair in settings. Create if it doesn't exist
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
