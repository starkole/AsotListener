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
    using System.Threading;

    public sealed class FileUtils : IFileUtils
    {
        private static StorageFolder localFolder = ApplicationData.Current.LocalFolder;
        private ILogger logger;

        private const string fileExtension = ".mp3";
        private const string partNumberDelimiter = "_";
        private const string mutexName = "AsotListener.FileUtils.Mutex";

        public FileUtils(ILogger logger)
        {
            this.logger = logger;
            logger.LogMessage("FileUtils initialized.");
        }
        #region Public Methods

        public async Task<IStorageFile> GetEpisodePartFile(string name, int partNumber)
        {
            try
            {
                logger.LogMessage($"FileUtils: getting file #{partNumber} for episode {name}");
                var filename = GetEpisodePartFilename(name, partNumber);
                return await localFolder.CreateFileAsync(filename, CreationCollisionOption.ReplaceExisting);
            }
            catch (Exception ex)
            {
                logger.LogMessage($"Exception while creating the file. {ex.Message}", LoggingLevel.Error);
                return null;
            }
        }

        public string GetEpisodePartFilename(string name, int partNumber) =>
            name + partNumberDelimiter + partNumber.ToString() + fileExtension;

        public async Task<IList<string>> GetDownloadedFileNamesList()
        {
            logger.LogMessage("Obtaining list of downloaded files...");
            var result = new List<string>();
            try
            {

                IReadOnlyList<StorageFile> files = await localFolder.GetFilesAsync();
                result = files?
                    .Where(f => f.FileType == fileExtension)
                    .Select(f => stripEndNumberFromFilename(f.DisplayName))
                    .Distinct()
                    .ToList() ?? result;
                logger.LogMessage($"Found {result.Count} files.");
            }
            catch (Exception ex)
            {
                logger.LogMessage($"Exception while getting downloaded file list. {ex.Message}", LoggingLevel.Error);
            }
            return result;
        }

        public async Task<IList<StorageFile>> GetFilesListForEpisode(string episodeName)
        {
            logger.LogMessage($"Obtaining file list for episode {episodeName}...");
            var result = new List<StorageFile>();
            try
            {
                IReadOnlyList<StorageFile> files = await localFolder.GetFilesAsync();
                result = files?
                    .Where(f => f.FileType == fileExtension && f.Name.StartsWith(episodeName))
                    .ToList() ?? result;
                logger.LogMessage($"Found {result.Count} files.");
            }
            catch (Exception ex)
            {
                logger.LogMessage($"Exception while getting episode file list. {ex.Message}", LoggingLevel.Error);
            }
            return result;
        }

        public async Task DeleteEpisode(string episodeName)
        {
            var files = await GetFilesListForEpisode(episodeName);
            if (files == null)
            {
                logger.LogMessage($"No files found to delete for episode {episodeName}.", LoggingLevel.Warning);
                return;
            }

            foreach (var file in files)
            {
                try
                {
                    logger.LogMessage($"Deleting file {file.Name}...");
                    await file.DeleteAsync();
                    logger.LogMessage("File deleted successfully.");
                }
                catch (Exception ex)
                {
                    logger.LogMessage($"Exception while deleting the file {file.Name}. {ex.Message}", LoggingLevel.Error);
                }
            }
        }

        public async Task SaveToXmlFile<T>(T objectToSave, string filename) where T : class
        {
            logger.LogMessage($"Serializing object of type {typeof(T)} to {filename}...");
            if (string.IsNullOrEmpty(filename))
            {
                logger.LogMessage("File name was not specified.", LoggingLevel.Error);
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

                logger.LogMessage("Serialization complete.");
            }
            catch (Exception e)
            {
                logger.LogMessage($"Error. Cannot serialize. {e.Message}", LoggingLevel.Error);
            }
        }

        public async Task<T> ReadFromXmlFile<T>(string filename) where T : class
        {
            logger.LogMessage($"Reading object of type {typeof(T)} from {filename}...");
            if (string.IsNullOrEmpty(filename))
            {
                logger.LogMessage("File name was not specified.", LoggingLevel.Error);
                return null;
            }

            try
            {
                StorageFile file = await ApplicationData.Current.LocalFolder.GetFileAsync(filename);
                using (IInputStream inStream = await file.OpenSequentialReadAsync())
                {
                    DataContractSerializer serializer = new DataContractSerializer(typeof(T));
                    T result = serializer.ReadObject(inStream.AsStreamForRead()) as T;
                    logger.LogMessage("Object has been successfully read from file.");
                    return result;
                }
            }
            catch (Exception e)
            {
                logger.LogMessage($"Error reading object from file. {e.Message}", LoggingLevel.Error);
                return null;
            }
        }

        public async Task TryDeleteFile(string filename)
        {
            logger.LogMessage($"Trying to delete file from {filename}...");
            try
            {
                var file = await localFolder.GetFileAsync(filename);
                if (file == null)
                {
                    logger.LogMessage("File not found.");
                    return;
                }

                await file.DeleteAsync();
                logger.LogMessage("File deleted successfully.");
            }
            catch (Exception ex)
            {
                logger.LogMessage($"Exception while deleting the file {filename}. {ex.Message}", LoggingLevel.Error);
            }
        }

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
            Regex numberAtTheEnd = new Regex("(" + fileExtension + ")$");
            string result = numberAtTheEnd.Replace(filenameWithExtension, string.Empty);
            return result;
        }

        #endregion
    }
}
