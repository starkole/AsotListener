namespace AsotListener.Services.Implementations
{
    using System;
    using System.Collections;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Linq;
    using Contracts;
    using Models;

    public class DownloadList : IDownloadList
    {
        private static BlockingCollection<Episode> episodes = new BlockingCollection<Episode>();
        private static Lazy<DownloadList> lazy = new Lazy<DownloadList>(() => new DownloadList()); 

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public static DownloadList Instance => lazy.Value;

        private DownloadList() { }

        #region Public Methods

        public bool TryAdd(Episode episode)
        {
            var result = episodes.TryAdd(episode);
            if (result)
            {
                CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add));
            }

            return result;
        }

        public bool TryGetFirst(out Episode episode)
        {
            if (!episodes.Any())
            {
                episode = null;
                return false;
            }

            var episodeToBeGot = episodes.ElementAt(0);
            if (TryRemove(episodeToBeGot.Name))
            {
                episode = episodeToBeGot;
                return true;
            }

            episode = null;
            return false;
        }

        public bool TryRemove(string episodeName)
        {
            var episode = episodes.Where(e => e.Name == episodeName).FirstOrDefault();
            if (episode == null)
            {
                // Already removed
                return true;
            }

            var result = episodes.TryTake(out episode);
            if (result)
            {
                CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove));
            }

            return result;
        }

        public IEnumerator<Episode> GetEnumerator() => ((IEnumerable<Episode>)episodes).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<Episode>)episodes).GetEnumerator();

        #endregion
    }
}
