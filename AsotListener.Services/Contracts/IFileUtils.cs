namespace AsotListener.Services.Contracts
{
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using Windows.Storage;

    public interface IFileUtils
    {
        string GetEpisodePartFilename(string name, int partNumber);
        Task<IList<string>> GetDownloadedFileNamesList();
        Task<IList<StorageFile>> GetFilesListForEpisode(string episodeName);
        Task<Stream> GetStreamForWriteToLocalFolder(string filename);
        Task DeleteEpisode(string episodeName);
        Task<T> ReadFromXmlFile<T>(string filename) where T : class;
        Task SaveToXmlFile<T>(T objectToSave, string filename) where T : class;
    }
}