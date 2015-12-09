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

        private static ILogger logger;

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
        }

        public async Task SavePlaylistToLocalStorage()
        {
            try
            {
                logger.LogMessage("Saving playlist to file...");
                MemoryStream listData = new MemoryStream();
                DataContractSerializer serializer = new DataContractSerializer(typeof(ObservableCollection<AudioTrack>));
                serializer.WriteObject(listData, TrackList);

                StorageFile file = await ApplicationData.Current.LocalFolder.CreateFileAsync(playlistFilename, CreationCollisionOption.ReplaceExisting);
                using (Stream fileStream = await file.OpenStreamForWriteAsync())
                {
                    listData.Seek(0, SeekOrigin.Begin);
                    await listData.CopyToAsync(fileStream);
                }

                logger.LogMessage("Playlist has been saved.");
            }
            catch (Exception e)
            {
                logger.LogMessage($"Cannot save playlist. {e.Message}", LoggingLevel.Error);
            }

            try
            {
                logger.LogMessage("Saving current track to file...");
                MemoryStream listData = new MemoryStream();
                DataContractSerializer serializer = new DataContractSerializer(typeof(AudioTrack));
                serializer.WriteObject(listData, CurrentTrack);

                StorageFile file = await ApplicationData.Current.LocalFolder.CreateFileAsync(currentTrackFilename, CreationCollisionOption.ReplaceExisting);
                using (Stream fileStream = await file.OpenStreamForWriteAsync())
                {
                    listData.Seek(0, SeekOrigin.Begin);
                    await listData.CopyToAsync(fileStream);
                }

                logger.LogMessage("Current track has been saved.");
            }
            catch (Exception e)
            {
                logger.LogMessage($"Cannot save current track. {e.Message}", LoggingLevel.Error);
            }
        }

        public async Task LoadPlaylistFromLocalStorage()
        {
            try
            {
                logger.LogMessage("Reading playlist from file...");
                StorageFile file = await ApplicationData.Current.LocalFolder.GetFileAsync(playlistFilename);
                using (IInputStream inStream = await file.OpenSequentialReadAsync())
                {
                    DataContractSerializer serializer = new DataContractSerializer(typeof(ObservableCollection<AudioTrack>));
                    TrackList = serializer.ReadObject(inStream.AsStreamForRead()) as ObservableCollection<AudioTrack>;
                }
                logger.LogMessage("Playlist has been successfully read from file.");
            }
            catch (Exception e)
            {
                logger.LogMessage($"Error reading playlist. {e.Message}", LoggingLevel.Error);
            }

            try
            {
                logger.LogMessage("Reading current track from file...");
                StorageFile file = await ApplicationData.Current.LocalFolder.GetFileAsync(currentTrackFilename);
                using (IInputStream inStream = await file.OpenSequentialReadAsync())
                {
                    DataContractSerializer serializer = new DataContractSerializer(typeof(AudioTrack));
                    CurrentTrack = serializer.ReadObject(inStream.AsStreamForRead()) as AudioTrack;
                }
                logger.LogMessage("Current track has been successfully read from file.");
            }
            catch (Exception e)
            {
                logger.LogMessage($"Error reading current track. {e.Message}", LoggingLevel.Error);
            }

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
