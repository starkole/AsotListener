namespace AsotListener.Services.Implementations
{
    using System.Collections.ObjectModel;
    using System.Threading.Tasks;
    using System.Linq;
    using Contracts;
    using Models;
    using Windows.Foundation.Diagnostics;
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
            logger.LogMessage("Playlist: Playlist initialized.", LoggingLevel.Information);
        }

        public async Task SavePlaylistToLocalStorage()
        {
            logger.LogMessage("Playlist: Saving playlist state to local storage...");
            await fileUtils.SaveToXmlFile(TrackList, playlistFilename);
            await fileUtils.SaveToXmlFile(CurrentTrack, currentTrackFilename);
            logger.LogMessage("Playlist: Playlist state saved.", LoggingLevel.Information);
        }

        public async Task LoadPlaylistFromLocalStorage()
        {
            logger.LogMessage("Playlist: Loading playlist state from local storage...");
            TrackList = await fileUtils.ReadFromXmlFile<ObservableCollection<AudioTrack>>(playlistFilename);
            CurrentTrack = await fileUtils.ReadFromXmlFile<AudioTrack>(currentTrackFilename);
            TrackList = TrackList ?? new ObservableCollection<AudioTrack>();
            logger.LogMessage("Playlist: Playlist loaded.", LoggingLevel.Information);
            if (CurrentTrack == null)
            {
                return;
            }

            var trackListItem = TrackList.FirstOrDefault(t => t.Uri == CurrentTrack.Uri);
            if (trackListItem == null)
            {
                logger.LogMessage("Playlist: Current track is not present in playlist. Adding it to playlist.", LoggingLevel.Warning);
                TrackList.Add(CurrentTrack);
                return;
            }

            trackListItem.StartPosition = CurrentTrack.StartPosition;
            CurrentTrack = trackListItem;
            logger.LogMessage("Playlist: Current track updated.", LoggingLevel.Information);
        }

        public string GetAudioTrackName(string episodeName, int partNumber) =>
            episodeName + trackNamePart + partNumber.ToString();
    }
}
