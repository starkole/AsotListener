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

    public class FileUtils : IFileUtils
    {
        private static Lazy<IFileUtils> lazy = new Lazy<IFileUtils>(() => new FileUtils());
        private static StorageFolder localFolder = ApplicationData.Current.LocalFolder;
        private ILogger logger;

        private const string fileExtension = ".mp3";
        private const string partNumberDelimiter = "_";

        public static IFileUtils Instance => lazy.Value;

        private FileUtils()
        {
            logger = Logger.Instance;
        }

        #region Public Methods

        public async Task<Stream> GetStreamForWriteToLocalFolder(string filename)
        {
            StorageFile file = await localFolder.CreateFileAsync(filename, CreationCollisionOption.ReplaceExisting);
            return await file.OpenStreamForWriteAsync();
        }

        public string GetEpisodePartFilename(string name, int partNumber) =>
            name + partNumberDelimiter + partNumber.ToString() + fileExtension;

        public async Task<IList<string>> GetDownloadedFileNamesList()
        {
            IReadOnlyList<StorageFile> files = await localFolder.GetFilesAsync();
            return files?
                .Where(f => f.FileType == fileExtension)
                .Select(f => stripEndNumberFromFilename(f.DisplayName))
                .Distinct()
                .ToList();
        }

        public async Task<IList<StorageFile>> GetFilesListForEpisode(string episodeName)
        {
            IReadOnlyList<StorageFile> files = await localFolder.GetFilesAsync();
            return files?
                .Where(f => f.FileType == fileExtension && f.Name.StartsWith(episodeName))
                .ToList();
        }

        public async Task DeleteEpisode(string episodeName)
        {
            var files = await GetFilesListForEpisode(episodeName);
            if (files == null)
            {
                return;
            }

            foreach (var file in files)
            {
                await file.DeleteAsync();
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
                return default(T);
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
                return default(T);
            }
        }

        #endregion

        #region Helper Methods

        private string stripEndNumberFromFilename(string filename)
        {
            Regex numberAtTheEnd = new Regex(partNumberDelimiter + "[0-9]+$");
            return numberAtTheEnd.Replace(filename, string.Empty);
        } 

        #endregion
    }
}
