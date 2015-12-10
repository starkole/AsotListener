namespace AsotListener.Services.Implementations
{
    using System;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Threading.Tasks;
    using Contracts;
    using Models;
    using Windows.Foundation.Diagnostics;
    using Windows.Storage;
    using Windows.Storage.Streams;

    public sealed class Playlist : BaseModel, IPlayList
    {
        private static Lazy<IPlayList> lazy = new Lazy<IPlayList>(() => new Playlist());
        private static ObservableCollection<AudioTrack> trackList = new ObservableCollection<AudioTrack>();
        private static AudioTrack currentTrack;
        private const string playlistFilename = "playlist.xml";
        private const string currentTrackFilename = "current_track.xml";

        private ILogger logger;
        private IFileUtils fileUtils;

        public static IPlayList Instance => lazy.Value;

        public ObservableCollection<AudioTrack> TrackList
        {
            get { return trackList; }
            private set { SetField(ref trackList, value, nameof(TrackList)); }
        }

        public AudioTrack CurrentTrack
        {
            get { return currentTrack; }
            set { SetField(ref currentTrack, value, nameof(CurrentTrack)); }
        }

        public int CurrentTrackIndex
        {
            get
            {
                if (CurrentTrack == null)
                {
                    return -1;
                }

                return TrackList.IndexOf(CurrentTrack);
            }
        }

        private Playlist()
        {
            // TODO: Use DI here
            logger = Logger.Instance;
            fileUtils = FileUtils.Instance;
        }

        public async Task SavePlaylistToLocalStorage()
        {
            await fileUtils.SaveToXmlFile(TrackList, playlistFilename);
            await fileUtils.SaveToXmlFile(CurrentTrack, currentTrackFilename);
        }

        public async Task LoadPlaylistFromLocalStorage()
        {
            TrackList = await fileUtils.ReadFromXmlFile<ObservableCollection<AudioTrack>>(playlistFilename);
            CurrentTrack = await fileUtils.ReadFromXmlFile<AudioTrack>(currentTrackFilename);

            if (TrackList == null || !TrackList.Any())
            {
                TrackList = new ObservableCollection<AudioTrack>();
            }

            if (CurrentTrack != null && !TrackList.Contains(currentTrack))
            {
                TrackList.Add(CurrentTrack);
            }
        }
    }
}
