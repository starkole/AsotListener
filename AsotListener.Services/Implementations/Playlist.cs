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
        private const string trackNamePart = " Part ";

        private readonly ILogger logger;
        private readonly IFileUtils fileUtils;

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
            logger.LogMessage("Playlist initialized.");
        }

        public async Task SavePlaylistToLocalStorage() // TODO implement separate saving of playlist and current track
        {
            logger.LogMessage("Saving playlist state to local storage...");
            await fileUtils.SaveToXmlFile(TrackList, playlistFilename);
            await fileUtils.SaveToXmlFile(CurrentTrack, currentTrackFilename);
            logger.LogMessage("Playlist state saved.");
        }

        public async Task LoadPlaylistFromLocalStorage()
        {
            logger.LogMessage("Loading playlist state from local storage...");
            TrackList = await fileUtils.ReadFromXmlFile<ObservableCollection<AudioTrack>>(playlistFilename);
            CurrentTrack = await fileUtils.ReadFromXmlFile<AudioTrack>(currentTrackFilename);
            logger.LogMessage("Playlist loaded.");
            TrackList = TrackList ?? new ObservableCollection<AudioTrack>();
            if (CurrentTrack != null && !TrackList.Contains(currentTrack))
            {
                logger.LogMessage(
                    "Current track is not present in playlist. Adding it to playlist.", 
                    Windows.Foundation.Diagnostics.LoggingLevel.Warning);
                TrackList.Add(CurrentTrack);
            }
        }

        public string GetAudioTrackName(string episodeName, int partNumber) =>
            episodeName + trackNamePart + partNumber.ToString();
    }
}
