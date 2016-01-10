namespace AsotListener.Services.Contracts
{
    using System.Threading.Tasks;
    using AsotListener.Models;
    using Common;

    public interface IDownloadManager : IAsyncInitialization
    {
        void CancelDownload(Episode episode);
        void ScheduleDownload(Episode episode);
        Task DownloadEpisode(Episode episode);
        Task RetrieveActiveDownloads();
    }
}