namespace AsotListener.Models
{
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.ComponentModel;
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using Windows.Storage;

    /// <summary>
    /// Holds current playlist and provides methods to manage it
    /// </summary>
    public sealed class Playlist : ObservableCollection<AudioTrack>
    {
        #region Fields

        private static Lazy<Playlist> lazy = new Lazy<Playlist>(() => new Playlist());

        private const string trackNamePart = " Part ";
        private int currentTrackIndex = -1;

        #endregion

        #region Events

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public new event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region Properties

        /// <summary>
        /// Returns playlist instance
        /// </summary>
        public static Playlist Instance => lazy.Value;

        /// <summary>
        /// Currently selected track
        /// </summary>
        public AudioTrack CurrentTrack
        {
            get { return Count == 0 || currentTrackIndex == -1 ? null : this[currentTrackIndex]; }
            set
            {
                if (value == null)
                {
                    if (currentTrackIndex != -1)
                    {
                        currentTrackIndex = -1;
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentTrack)));
                        return;
                    }
                }

                var existingTrack = this.FirstOrDefault(t => t.Name == value.Name);
                if (existingTrack == null)
                {
                    Add(value);
                    currentTrackIndex = IndexOf(value);
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentTrack)));
                    return;
                }

                int index = IndexOf(existingTrack);
                if (currentTrackIndex != index)
                {
                    currentTrackIndex = index;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentTrack)));
                }
            }
        }

        /// <summary>
        /// Index of currently selected track
        /// </summary>
        public int CurrentTrackIndex
        {
            get
            {
                return currentTrackIndex;
            }
            set
            {
                if (Count == 0)
                {
                    currentTrackIndex = -1;
                    return;
                }

                int newIndex = value < 0 ? (Count - value) % Count : value % Count;
                if (newIndex != currentTrackIndex)
                {
                    currentTrackIndex = newIndex;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentTrack)));
                }
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Constructs the name of audio track from given parameters
        /// </summary>
        /// <param name="episodeName">Episode name</param>
        /// <param name="partNumber">The part number of current track</param>
        /// <returns>The name of audio track</returns>
        public static string GetAudioTrackName(string episodeName, int partNumber) =>
            episodeName + trackNamePart + partNumber.ToString();

        /// <summary>
        /// Adds several items to <see cref="Playlist"/>
        /// </summary>
        /// <param name="newItems">Items to add</param>
        public void AddRange(IEnumerable<AudioTrack> newItems)
        {
            if (newItems == null)
            {
                return;
            }

            bool playlistChanged = false;
            foreach (var item in newItems)
            {
                if (!Contains(item))
                {
                    Items.Add(item);
                    playlistChanged = true;
                }
            }

            if (playlistChanged)
            {
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, newItems.ToList()));
            }
        }

        /// <summary>
        /// Adds all files belonging to certain episode to playlist
        /// </summary>
        /// <param name="episodeName">Episode name</param>
        /// <param name="episodeFiles">The list of episode files</param>
        /// <returns>The first <see cref="AudioTrack"/> added to playlist</returns>
        public AudioTrack AddEpisodeFiles(string episodeName, IList<StorageFile> episodeFiles)
        {
            var tracks = episodeFiles
                .Select(f => new AudioTrack(episodeName)
                {
                    Name = GetAudioTrackName(episodeName, episodeFiles.IndexOf(f)),
                    Uri = f.Path
                })
                .ToList();
            AddRange(tracks);
            return tracks.FirstOrDefault();
        }

        /// <summary>
        /// Determines whether an <see cref="AudioTrack"/> is in <see cref="Playlist"/>.
        /// </summary>
        /// <param name="item">
        /// The <see cref="AudioTrack"/> to locate in the <see cref="Playlist"/>. The value can be null.
        /// </param>
        /// <returns>
        /// true if <see cref="AudioTrack"/> is found in the <see cref="Playlist"/>; otherwise, false
        /// </returns>
        public new bool Contains(AudioTrack item) => this.Any(t => t.Name == item?.Name);

        /// <summary>
        /// Determines whether an <see cref="Episode"/> is in <see cref="Playlist"/>.
        /// </summary>
        /// <param name="item">
        /// The <see cref="Episode"/> to locate in the <see cref="Playlist"/>. The value can be null.
        /// </param>
        /// <returns>
        /// true if <see cref="Episode"/> is found in the <see cref="Playlist"/>; otherwise, false
        /// </returns>
        public bool Contains(Episode episode) => this.Any(t => t.EpisodeName == episode?.Name);

        #endregion
    }
}
