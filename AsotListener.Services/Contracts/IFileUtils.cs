namespace AsotListener.Services.Contracts
{
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using Windows.Storage;

    public interface IFileUtils
    {
        string CreateFilename(string name, int partNumber);
        Task<IList<string>> GetDownloadedFileNamesList();
        Task<IList<StorageFile>> GetFilesListForEpisode(string episodeName);
        Task<Stream> GetStreamForWrite(string filename);
        Task DeleteEpisode(string episodeName);
    }
}