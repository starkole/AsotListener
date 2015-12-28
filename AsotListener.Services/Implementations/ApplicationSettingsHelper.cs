namespace AsotListener.Services.Implementations
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Linq;
    using Contracts;
    using Models;
    using Windows.Foundation.Diagnostics;
    using Windows.Storage;
    using System.Collections.Generic;
    using Common;

    /// <summary>
    /// Helper class to read and write settings to LocalSettings
    /// </summary>
    public sealed class ApplicationSettingsHelper : IApplicationSettingsHelper
    {
        private readonly ILogger logger;
        private readonly IFileUtils fileUtils;

        private const string mutexName = "AsotListener.ApplicationSettingsHelper.Mutex";
        private const string playlistFilename = "playlist.xml";
        private const int mutexTimeout = 2000;

        /// <summary>
        /// Creates an instance of <see cref="ApplicationSettingsHelper"/> class
        /// </summary>
        /// <param name="logger">The logger instance</param>
        public ApplicationSettingsHelper(ILogger logger, IFileUtils fileUtils)
        {
            this.logger = logger;
            this.fileUtils = fileUtils;
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
            logger.LogMessage($"Reading {key} parameter from LoaclSettings.");
            var mutex = new Mutex(false, mutexName);
            try
            {
                mutex.WaitOne(mutexTimeout);
                if (!ApplicationData.Current.LocalSettings.Values.ContainsKey(key))
                {
                    logger.LogMessage($"No {key} parameter found in LoaclSettings.", LoggingLevel.Warning);
                    return default(T);
                }

                var value = (T)ApplicationData.Current.LocalSettings.Values[key];
                return value;
            }
            catch (Exception ex)
            {
                logger.LogMessage($"Exception on reading from LoaclSettings. {ex.Message}", LoggingLevel.Error);
                return default(T);
            }
            finally
            {
                mutex.ReleaseMutex();
                mutex.Dispose();
            }
        }

        /// <summary>
        /// Save a key-value pair in settings. Create if it doesn't exist
        /// </summary>
        public void SaveSettingsValue(string key, object value)
        {
            logger.LogMessage($"Saving {key} parameter to LoaclSettings.");
            var mutex = new Mutex(false, mutexName);
            try
            {
                mutex.WaitOne(mutexTimeout);
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
                mutex.Dispose();
            }

        }

        public async Task SavePlaylist()
        {
            logger.LogMessage($"Saving playlist with {Playlist.Instance.Count} tracks...", LoggingLevel.Information);
            SaveSettingsValue(Keys.CurrentTrack, Playlist.Instance.CurrentTrackIndex);
            await fileUtils.SaveToXmlFile(Playlist.Instance.ToList(), playlistFilename);
            logger.LogMessage($"Playlist saved.");
        }


        public async Task LoadPlaylist()
        {
            var tracks = await fileUtils.ReadFromXmlFile<List<AudioTrack>>(playlistFilename);
            int count = tracks?.Count ?? -1;
            logger.LogMessage($"Loaded playlist with {count} tracks.", LoggingLevel.Information);
            int currentTrackIndex = ReadSettingsValue<int>(Keys.CurrentTrack);
            Playlist.Instance.Clear();
            Playlist.Instance.AddRange(tracks);
            Playlist.Instance.CurrentTrackIndex = currentTrackIndex;
        }
    }
}
