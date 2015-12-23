namespace AsotListener.Services.Implementations
{
    using System.Collections.ObjectModel;
    using System.Threading.Tasks;
    using System.Linq;
    using Contracts;
    using Models;
    using Windows.Foundation.Diagnostics;

    /// <summary>
    /// Holds current playlist and provides methods to manage it
    /// </summary>
    public sealed class Playlist : BaseModel, IPlayList
    {
        private ObservableCollection<AudioTrack> trackList = new ObservableCollection<AudioTrack>();
        private AudioTrack currentTrack;
        private const string playlistFilename = "playlist.xml";
        private const string currentTrackFilename = "current_track.xml";
        private const string trackNamePart = " Part ";

        private readonly ILogger logger;
        private readonly IFileUtils fileUtils;

        /// <summary>
        /// The list of <see cref="AudioTrack"/> in current playlist
        /// </summary>
        public ObservableCollection<AudioTrack> TrackList
        {
            get { return trackList; }
            private set { SetField(ref trackList, value, nameof(TrackList)); }
        }

        /// <summary>
        /// Currently selected track
        /// </summary>
        public AudioTrack CurrentTrack
        {
            get { return currentTrack; }
            set { SetField(ref currentTrack, value, nameof(CurrentTrack)); }
        }

        /// <summary>
        /// Index of currently selected track
        /// </summary>
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

        /// <summary>
        /// Creates instance of <see cref="Playlist"/>
        /// </summary>
        /// <param name="logger">The logger instance</param>
        /// <param name="fileUtils">The instance of file utils class</param>
        public Playlist(ILogger logger, IFileUtils fileUtils)
        {
            this.logger = logger;
            this.fileUtils = fileUtils;
            logger.LogMessage("Playlist: Playlist initialized.", LoggingLevel.Information);
        }

        /// <summary>
        /// Saves playlist state to local storage
        /// </summary>
        /// <returns>Awaitable <see cref="Task"/></returns>
        public async Task SavePlaylistToLocalStorage()
        {
            logger.LogMessage("Playlist: Saving playlist state to local storage...");
            await fileUtils.SaveToXmlFile(TrackList, playlistFilename);
            await fileUtils.SaveToXmlFile(CurrentTrack, currentTrackFilename);
            logger.LogMessage("Playlist: Playlist state saved.", LoggingLevel.Information);
        }

        /// <summary>
        /// Loads playlist data from local storage and restores playlist state
        /// </summary>
        /// <returns>Awaitable <see cref="Task"/></returns>
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

        /// <summary>
        /// Constructs the name of audio track from given parameters
        /// </summary>
        /// <param name="episodeName">Episode name</param>
        /// <param name="partNumber">The part number of current track</param>
        /// <returns>The name of audio track</returns>
        public string GetAudioTrackName(string episodeName, int partNumber) =>
            episodeName + trackNamePart + partNumber.ToString();
    }
}
