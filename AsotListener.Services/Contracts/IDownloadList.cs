namespace AsotListener.Services.Contracts
{
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using Models;

    public interface IDownloadList: INotifyCollectionChanged, IEnumerable<Episode>
    {
        bool TryAdd(Episode episode);
        bool TryGetFirst(out Episode episode);
        bool TryRemove(string episodeName);
    }
}