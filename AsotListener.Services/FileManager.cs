namespace AsotListener.Services
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using Windows.Storage;
    using System.Linq;
    using System.Text.RegularExpressions;

    public static class FileManager
    {
        private const string FILE_EXTENSION = ".mp3";

        private static StorageFolder localFolder = ApplicationData.Current.LocalFolder;

        public static async Task<Stream> GetStreamForWrite(string name, int? partNumber = null)
        {            
            string filename = createFilename(name, partNumber);
            StorageFile file = await localFolder.CreateFileAsync(filename, CreationCollisionOption.ReplaceExisting);
            return await file.OpenStreamForWriteAsync();
        }

        private static string createFilename(string name, int? partNumber)
        {
            if (partNumber == null)
            {
                return name + FILE_EXTENSION;
            }

            return name + partNumber.ToString() + FILE_EXTENSION;
        }

        /// <summary>
        /// Returns list of files that have already been downloaded and exist on the phone
        /// </summary>
        /// <returns>List of files or null, when no files has been found</returns>
        public static async Task<IList<string>> GetDownloadedFileNamesList()
        {
            IReadOnlyList<StorageFile> files = await localFolder.GetFilesAsync();
            return files?
                .Where(f => f.FileType == FILE_EXTENSION)
                .Select(f => stripEndNumberFromFilename(f.DisplayName))
                .Distinct()
                .ToList();
        }
        
        private static string stripEndNumberFromFilename(string filename)
        {
            // TODO: Think about the case, then there is more than 9 download links 
            Regex numberAtTheEnd = new Regex("[0-9]$");
            return numberAtTheEnd.Replace(filename, string.Empty);
        }
    }
}
