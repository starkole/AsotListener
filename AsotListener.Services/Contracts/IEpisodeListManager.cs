namespace AsotListener.Services.Contracts
{
    using System.Threading.Tasks;
    using Models;
    using Common;

    public interface IEpisodeListManager: IAsyncInitialization
    {
        Task DeleteEpisodeDataAsync(Episode episode);
        Task PlayEpisodeAsync(Episode episode);
        Task AddEpisodeToPLaylistAsync(Episode episode);
        Task UpdateEpisodeStatesAsync();
        Task<int> LoadEpisodeListFromServerAsync();
        Episode GetEpisodeByNumber(int number);
        Task PlayLastDownloadedEpisodeAsync();
    }
}
