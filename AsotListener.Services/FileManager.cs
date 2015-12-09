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
        private const string fileExtension = ".mp3";
        public const string partNumberDelimiter = "_";
        public const string filePathPrefix = @"ms-appdata:///";

        private static StorageFolder localFolder = ApplicationData.Current.LocalFolder;

        public static async Task<Stream> GetStreamForWrite(string filename)
        {            
            StorageFile file = await localFolder.CreateFileAsync(filename, CreationCollisionOption.ReplaceExisting);
            return await file.OpenStreamForWriteAsync();
        }

        public static string createFilename(string name, int partNumber) => name + partNumberDelimiter + partNumber.ToString() + fileExtension;

        public static async Task<IList<string>> GetDownloadedFileNamesList()
        {
            IReadOnlyList<StorageFile> files = await localFolder.GetFilesAsync();
            return files?
                .Where(f => f.FileType == fileExtension)
                .Select(f => stripEndNumberFromFilename(f.DisplayName))
                .Distinct()
                .ToList();
        }

        public static string stripEndNumberFromFilename(string filename)
        {
            Regex numberAtTheEnd = new Regex(partNumberDelimiter + "[0-9]+$");
            return numberAtTheEnd.Replace(filename, string.Empty);
        }
    }
}
