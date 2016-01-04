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
    using Windows.Media.Playback;
    using Windows.Foundation.Collections;

    /// <summary>
    /// Helper class to read and write settings to LocalSettings
    /// </summary>
    public sealed class ApplicationSettingsHelper : IApplicationSettingsHelper
    {
        private readonly ILogger logger;
        private readonly IFileUtils fileUtils;

        private const string mutexName = "AsotListener.ApplicationSettingsHelper.Mutex";
        private const string playlistFilename = "playlist.xml";
        private const string episodeListFileName = "episodeList.xml";
        private const int mutexTimeout = 2000;

        /// <summary>
        /// The result of the asynchronous initialization.
        /// </summary>
        public Task Initialization { get; }

        /// <summary>
        /// Creates an instance of <see cref="ApplicationSettingsHelper"/> class
        /// </summary>
        /// <param name="logger">The logger instance</param>
        public ApplicationSettingsHelper(ILogger logger, IFileUtils fileUtils)
        {
            this.logger = logger;
            this.fileUtils = fileUtils;
            Initialization = initializeAsync();
            logger.LogMessage("ApplicationSettingsHelper initialized.", LoggingLevel.Information);
        }

        private async Task initializeAsync()
        {
            await LoadPlaylist();
            await LoadEpisodeList();
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

        /// <summary>
        /// Saves current playlist to file
        /// </summary>
        /// <returns>Task which completes after playlist has been saved</returns>
        public async Task SavePlaylist()
        {
            logger.LogMessage($"Saving playlist with {Playlist.Instance.Count} tracks...", LoggingLevel.Information);
            SaveSettingsValue(Keys.CurrentTrack, Playlist.Instance.CurrentTrackIndex);
            await fileUtils.SaveToXmlFile(Playlist.Instance.ToList(), playlistFilename);
            logger.LogMessage($"Playlist saved.");
        }

        /// <summary>
        /// Saves current playlist to file and notifies BackgroundAudio task about it.
        /// </summary>
        /// <returns>Task which completes after playlist has been saved</returns>
        public async Task SavePlaylistWithNotification()
        {
            await SavePlaylist();
            BackgroundMediaPlayer.SendMessageToBackground(new ValueSet { { Keys.PlaylistUpdated, null } });
        }

        /// <summary>
        /// Loads current playlist from file
        /// </summary>
        /// <returns>Task which completes after playlist has been loaded </returns>
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

        /// <summary>
        /// Saves current episode list to file
        /// </summary>
        /// <returns>Task which completes after episode list has been saved </returns>
        public async Task SaveEpisodeList()
        {
            logger.LogMessage($"Saving episode list with {EpisodeList.Instance.Count} episodes...", LoggingLevel.Information);
            await fileUtils.SaveToXmlFile(EpisodeList.Instance.ToList(), episodeListFileName);
            logger.LogMessage($"Episode list saved.");
        }

        /// <summary>
        /// Loads current episode list from file
        /// </summary>
        /// <returns>Task which completes after episode list has been loaded </returns>
        public async Task LoadEpisodeList()
        {
            var episodes = await fileUtils.ReadFromXmlFile<List<Episode>>(episodeListFileName);
            int count = episodes?.Count ?? -1;
            logger.LogMessage($"Loaded episode list with {count} episodes.", LoggingLevel.Information);
            EpisodeList.Instance.Clear();
            EpisodeList.Instance.AddRange(episodes);
        }
    }
}
