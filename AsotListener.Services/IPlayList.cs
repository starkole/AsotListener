namespace AsotListener.Services
{
    using System.Collections.ObjectModel;
    using Models;

    public interface IPlayList
    {
        ObservableCollection<AudioTrack> TrackList { get; }
        int CurrentTrackIndex { get; }
        AudioTrack CurrentTrack { get; set; }
        void SavePlaylistToLocalStorage();
        void LoadPlaylistFromLocalStorage();
    }
}