namespace AsotListener.Services.Implementations
{
    using System.Collections.ObjectModel;
    using System.Threading.Tasks;
    using Contracts;
    using Models;

    public sealed class Playlist : BaseModel, IPlayList
    {
        private ObservableCollection<AudioTrack> trackList = new ObservableCollection<AudioTrack>();
        private AudioTrack currentTrack;
        private const string playlistFilename = "playlist.xml";
        private const string currentTrackFilename = "current_track.xml";

        private ILogger logger;
        private IFileUtils fileUtils;

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

        public Playlist(ILogger logger, IFileUtils fileUtils)
        {
            this.logger = logger;
            this.fileUtils = fileUtils;
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
            TrackList = TrackList ?? new ObservableCollection<AudioTrack>();
            if (CurrentTrack != null && !TrackList.Contains(currentTrack))
            {
                TrackList.Add(CurrentTrack);
            }
        }
    }
}
