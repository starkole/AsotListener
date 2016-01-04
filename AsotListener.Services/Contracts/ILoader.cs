namespace AsotListener.Services.Contracts
{
    using System;
    using System.Threading.Tasks;
    using Models;

    public interface ILoader: IDisposable
    {
        Task FetchEpisodeListAsync();
        Task<string> FetchEpisodePageAsync(Episode episode);
    }
}