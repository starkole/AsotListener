namespace AsotListener.Services.Contracts
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Windows.Storage;

    public interface IFileUtils
    {
        string GetEpisodePartFilename(string name, int partNumber);
        string ExtractEpisodeNameFromFilename(string filenameWithExtension);
        Task<IStorageFile> CreateEpisodePartFile(string name, int partNumber);
        Task<IList<string>> GetDownloadedFileNamesList();
        Task<IList<StorageFile>> GetFilesListForEpisode(string episodeName);
        Task<T> ReadFromXmlFile<T>(string filename) where T : class;
        Task SaveToXmlFile<T>(T objectToSave, string filename) where T : class;
        Task DeleteEpisode(string episodeName);
        Task TryDeleteFile(string filename);
    }
}