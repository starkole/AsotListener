namespace AsotListener.Services
{
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;

    public interface IFileUtils
    {
        string FilePathPrefix { get; }

        string CreateFilename(string name, int partNumber);
        Task<IList<string>> GetDownloadedFileNamesList();
        Task<Stream> GetStreamForWrite(string filename);
    }
}