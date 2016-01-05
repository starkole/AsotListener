namespace AsotListener.Services.Contracts
{
    using System.Threading.Tasks;
    using Models;
    using Common;

    public interface IEpisodeListManager: IAsyncInitialization
    {
        Task DeleteEpisodeData(Episode episode);
        Task PlayEpisode(Episode episode);
        Task AddEpisodeToPLaylist(Episode episode);
        Task UpdateEpisodeStates();
        Task LoadEpisodeListFromServer();
    }
}
