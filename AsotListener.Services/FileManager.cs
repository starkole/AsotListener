namespace AsotListener.Services
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using Windows.Storage;

    public class FileManager : IFileManager
    {
        private const string DEFAULT_FILENAME = "episode";
        private const string FILE_EXTENSION = ".mp3";

        private StorageFolder localFolder;

        public FileManager()
        {
            localFolder = ApplicationData.Current.LocalFolder;
        }

        public async Task<Stream> GetStreamForWrite(int partNumber)
        {
            string filename = createFilename(partNumber);
            StorageFile file = await localFolder.CreateFileAsync(filename, CreationCollisionOption.ReplaceExisting);
            return await file.OpenStreamForWriteAsync();
        }

        private string createFilename(int partNumber)
        {
            return DEFAULT_FILENAME + partNumber.ToString() + FILE_EXTENSION;
        }
    }
}
