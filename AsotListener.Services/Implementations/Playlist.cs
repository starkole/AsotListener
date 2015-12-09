namespace AsotListener.Services.Implementations
{
    using System;
    using System.Collections.ObjectModel;
    using Contracts;
    using Models;

    public sealed class Playlist: BaseModel, IPlayList
    {
        private static Lazy<IPlayList> lazy = new Lazy<IPlayList>(() => new Playlist());
        private static ObservableCollection<AudioTrack> trackList = new ObservableCollection<AudioTrack>();
        private static AudioTrack currentTrack;

        // TODO: Use DI here
        private static IApplicationSettingsHelper settingsHelper = ApplicationSettingsHelper.Instance;

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

        private Playlist() { }
        
        public void SavePlaylistToLocalStorage()
        {
            settingsHelper.SaveSettingsValue(Constants.Playlist, TrackList);
            settingsHelper.SaveSettingsValue(Constants.CurrentTrack, currentTrack);
        }

        public void LoadPlaylistFromLocalStorage()
        {
            TrackList = (settingsHelper.ReadAndRemoveSettingsValue(Constants.Playlist) as ObservableCollection<AudioTrack>) ?? 
                new ObservableCollection<AudioTrack>();
            CurrentTrack = settingsHelper.ReadAndRemoveSettingsValue(Constants.CurrentTrack) as AudioTrack;
            if (CurrentTrack != null && !TrackList.Contains(currentTrack))
            {
                TrackList.Add(CurrentTrack);
            }
        }
    }
}
