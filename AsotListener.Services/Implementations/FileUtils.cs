namespace AsotListener.Services.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using Windows.Storage;
    using System.Linq;
    using System.Text.RegularExpressions;
    using Contracts;
    using System.Runtime.Serialization;
    using Windows.Foundation.Diagnostics;
    using Windows.Storage.Streams;

    /// <summary>
    /// Contains helper methods to work with files in local folder
    /// </summary>
    public sealed class FileUtils : IFileUtils
    {
        #region Fields

        private static StorageFolder localFolder = ApplicationData.Current.LocalFolder;

        private const string audioFileExtension = ".mp3";
        private const string partNumberDelimiter = "_";
        private const string mutexName = "AsotListener.FileUtils.Mutex";

        private ILogger logger;

        #endregion
        
        #region Ctor

        /// <summary>
        /// Creates instance of <see cref="FileUtils"/> class
        /// </summary>
        /// <param name="logger">The logger instance</param>
        public FileUtils(ILogger logger)
        {
            this.logger = logger;
            logger.LogMessage("FileUtils initialized.", LoggingLevel.Information);
        }

        #endregion
        
        #region Public Methods

        /// <summary>
        /// Creates new file with name based on given name and part number
        /// </summary>
        /// <param name="name">Episode name</param>
        /// <param name="partNumber">Episode part number</param>
        /// <returns>Newly created file instance or null in case of any error</returns>
        public async Task<IStorageFile> CreateEpisodePartFile(string name, int partNumber)
        {
            try
            {
                logger.LogMessage($"FileUtils: getting file #{partNumber} for episode {name}");
                var filename = GetEpisodePartFilename(name, partNumber);
                return await localFolder.CreateFileAsync(filename, CreationCollisionOption.ReplaceExisting);
            }
            catch (Exception ex)
            {
                logger.LogMessage($"FileUtils: Exception while creating the file. {ex.Message}", LoggingLevel.Error);
                return null;
            }
        }

        /// <summary>
        /// Constructs filename for episode part based on given data
        /// </summary>
        /// <param name="name">Episode name</param>
        /// <param name="partNumber">Episode part number</param>
        /// <returns>Filename for episode</returns>
        public string GetEpisodePartFilename(string name, int partNumber) =>
            name + partNumberDelimiter + partNumber.ToString() + audioFileExtension;

        /// <summary>
        /// Searches for downloaded audio files in application local folder
        /// </summary>
        /// <returns>The distinct list of audio file names wthout extension and part number</returns>
        public async Task<IList<string>> GetDownloadedFileNamesList()
        {
            logger.LogMessage("FileUtils: Obtaining list of downloaded files...");
            var result = new List<string>();
            try
            {
                IReadOnlyList<StorageFile> files = await localFolder.GetFilesAsync();
                result = files?
                    .Where(f => f.FileType == audioFileExtension)
                    .Select(f => stripEndNumberFromFilename(f.DisplayName))
                    .Distinct()
                    .ToList() ?? result;
                logger.LogMessage($"FileUtils: Found {result.Count} downloaded files.", LoggingLevel.Information);
            }
            catch (Exception ex)
            {
                logger.LogMessage($"FileUtils: Exception while getting downloaded file list. {ex.Message}", LoggingLevel.Error);
            }
            return result;
        }

        /// <summary>
        /// Searches for downloaded audio files in application local folder for given episode
        /// </summary>
        /// <param name="episodeName">Episode name</param>
        /// <returns>List of files for given episode</returns>
        public async Task<IList<StorageFile>> GetFilesListForEpisode(string episodeName)
        {
            logger.LogMessage($"FileUtils: Obtaining file list for episode {episodeName}...");
            var result = new List<StorageFile>();
            try
            {
                IReadOnlyList<StorageFile> files = await localFolder.GetFilesAsync();
                result = files?
                    .Where(f => f.FileType == audioFileExtension && f.Name.StartsWith(episodeName, StringComparison.CurrentCulture))
                    .ToList() ?? result;
                logger.LogMessage($"FileUtils: Found {result.Count} files.");
            }
            catch (Exception ex)
            {
                logger.LogMessage($"FileUtils: Exception while getting episode file list. {ex.Message}", LoggingLevel.Error);
            }
            return result;
        }

        /// <summary>
        /// Deletes all downloaded files for given episode
        /// </summary>
        /// <param name="episodeName">Episode name</param>
        /// <returns>Awaitable <see cref="Task"/></returns>
        public async Task DeleteEpisode(string episodeName)
        {
            var files = await GetFilesListForEpisode(episodeName);
            if (files == null)
            {
                logger.LogMessage($"FileUtils: No files found to delete for episode {episodeName}.", LoggingLevel.Warning);
                return;
            }

            foreach (var file in files)
            {
                try
                {
                    logger.LogMessage($"FileUtils: Deleting file {file.Name}...");
                    await file.DeleteAsync();
                    logger.LogMessage("FileUtils: File deleted successfully.");
                }
                catch (Exception ex)
                {
                    logger.LogMessage($"FileUtils: Exception while deleting the file {file.Name}. {ex.Message}", LoggingLevel.Error);
                }
            }
        }

        /// <summary>
        /// Serializes given object to XML and saves it to file
        /// </summary>
        /// <typeparam name="T">The object type</typeparam>
        /// <param name="objectToSave">Object to save</param>
        /// <param name="filename">File name</param>
        /// <returns>Awaitable <see cref="Task"/></returns>
        public async Task SaveToXmlFile<T>(T objectToSave, string filename) where T : class
        {
            logger.LogMessage($"FileUtils: Serializing object of type {typeof(T)} to {filename}...");
            if (string.IsNullOrEmpty(filename))
            {
                logger.LogMessage("FileUtils: File name was not specified.", LoggingLevel.Error);
                return;
            }

            try
            {
                MemoryStream listData = new MemoryStream();
                DataContractSerializer serializer = new DataContractSerializer(typeof(T));
                serializer.WriteObject(listData, objectToSave);
                StorageFile file = await ApplicationData.Current.LocalFolder.CreateFileAsync(filename, CreationCollisionOption.ReplaceExisting);
                using (Stream fileStream = await file.OpenStreamForWriteAsync())
                {
                    listData.Seek(0, SeekOrigin.Begin);
                    await listData.CopyToAsync(fileStream);
                }

                logger.LogMessage("FileUtils: Serialization complete.");
            }
            catch (Exception e)
            {
                logger.LogMessage($"FileUtils: Error. Cannot serialize. {e.Message}", LoggingLevel.Error);
            }
        }

        /// <summary>
        /// Reads from file and deserializes previously saved object
        /// </summary>
        /// <typeparam name="T">Object type</typeparam>
        /// <param name="filename">File name</param>
        /// <returns>Object of type <see cref="T"/></returns>
        public async Task<T> ReadFromXmlFile<T>(string filename) where T : class
        {
            logger.LogMessage($"FileUtils: Reading object of type {typeof(T)} from {filename}...");
            if (string.IsNullOrEmpty(filename))
            {
                logger.LogMessage("FileUtils: File name was not specified.", LoggingLevel.Error);
                return null;
            }

            try
            {
                StorageFile file = await ApplicationData.Current.LocalFolder.GetFileAsync(filename);
                using (IInputStream inStream = await file.OpenSequentialReadAsync())
                {
                    DataContractSerializer serializer = new DataContractSerializer(typeof(T));
                    T result = serializer.ReadObject(inStream.AsStreamForRead()) as T;
                    logger.LogMessage("FileUtils: Object has been successfully read from file.");
                    return result;
                }
            }
            catch (Exception e)
            {
                logger.LogMessage($"FileUtils: Error reading object from file. {e.Message}", LoggingLevel.Error);
                return null;
            }
        }

        /// <summary>
        /// Tries to delete given file
        /// </summary>
        /// <param name="filename">File name</param>
        /// <returns>Awaitable <see cref="Task"/></returns>
        public async Task TryDeleteFile(string filename)
        {
            logger.LogMessage($"FileUtils: Trying to delete file from {filename}...");
            try
            {
                var file = await localFolder.GetFileAsync(filename);
                if (file == null)
                {
                    logger.LogMessage("FileUtils: File not found.");
                    return;
                }

                await file.DeleteAsync();
                logger.LogMessage("FileUtils: File deleted successfully.");
            }
            catch (Exception ex)
            {
                logger.LogMessage($"FileUtils: Exception while deleting the file {filename}. {ex.Message}", LoggingLevel.Error);
            }
        }

        /// <summary>
        /// Extracts episode name from given filename 
        /// </summary>
        /// <param name="filenameWithExtension">File name with extension</param>
        /// <returns>Episode name</returns>
        public string ExtractEpisodeNameFromFilename(string filenameWithExtension)
        {
            string filename = stripExtensionFromFilename(filenameWithExtension);
            string result = stripEndNumberFromFilename(filename);
            return result;
        }

        #endregion

        #region Helper Methods

        private string stripEndNumberFromFilename(string filename)
        {
            Regex numberAtTheEnd = new Regex(partNumberDelimiter + "[0-9]+$");
            string result = numberAtTheEnd.Replace(filename, string.Empty);
            return result;
        }

        private string stripExtensionFromFilename(string filenameWithExtension)
        {
            Regex numberAtTheEnd = new Regex("(" + audioFileExtension + ")$");
            string result = numberAtTheEnd.Replace(filenameWithExtension, string.Empty);
            return result;
        }

        #endregion
    }
}
