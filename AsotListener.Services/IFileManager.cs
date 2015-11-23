namespace AsotListener.Services
{
    using System.IO;
    using System.Threading.Tasks;

    public interface IFileManager
    {
        Task<Stream> GetStreamForWrite(int partNumber);
    }
}
