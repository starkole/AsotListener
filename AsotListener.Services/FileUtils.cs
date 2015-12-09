namespace AsotListener.Services
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using Windows.Storage;
    using System.Linq;
    using System.Text.RegularExpressions;
    using Models;

    public class FileUtils : IFileUtils
    {
        private static Lazy<FileUtils> lazy = new Lazy<FileUtils>(() => new FileUtils());
        private static StorageFolder localFolder = ApplicationData.Current.LocalFolder;

        private const string fileExtension = ".mp3";
        private const string partNumberDelimiter = "_";

        public static IFileUtils Instance => lazy.Value;

        private FileUtils() { }

        public async Task<Stream> GetStreamForWrite(string filename)
        {
            StorageFile file = await localFolder.CreateFileAsync(filename, CreationCollisionOption.ReplaceExisting);
            return await file.OpenStreamForWriteAsync();
        }

        public string CreateFilename(string name, int partNumber) => 
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

        public async void DeleteEpisode(string episodeName)
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

        private string stripEndNumberFromFilename(string filename)
        {
            Regex numberAtTheEnd = new Regex(partNumberDelimiter + "[0-9]+$");
            return numberAtTheEnd.Replace(filename, string.Empty);
        }
    }
}
