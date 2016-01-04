namespace AsotListener.Models
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;

    /// <summary>
    /// Represents a list of episodes.
    /// </summary>
    public sealed class EpisodeList : ObservableCollection<Episode>
    {
        private static Lazy<EpisodeList> lazy = new Lazy<EpisodeList>(() => new EpisodeList());

        /// <summary>
        /// Returns current instance of <see cref="EpisodeList"/>
        /// </summary>
        public static EpisodeList Instance => lazy.Value;

        private EpisodeList() { }

        /// <summary>
        /// Adds new items to episode list
        /// </summary>
        /// <param name="newItems">Items to add. Can be empty or null - will be ignored in such a case.</param>
        public void AddRange(IEnumerable<Episode> newItems)
        {
            if (newItems != null && newItems.Any())
            {
                int lastItemIndex = Items.Count - 1;
                lastItemIndex = lastItemIndex < 0 ? 0 : lastItemIndex;
                foreach (var item in newItems)
                {
                    Items.Add(item);
                }

                IList<Episode> itemsList = newItems.ToList();
                var args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, itemsList, lastItemIndex);
                OnCollectionChanged(args);
            }
        }
    }
}
