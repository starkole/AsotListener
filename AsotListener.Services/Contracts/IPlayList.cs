namespace AsotListener.Services.Contracts
{
    using System.Collections.ObjectModel;
    using System.Threading.Tasks;
    using Models;

    public interface IPlayList
    {
        ObservableCollection<AudioTrack> TrackList { get; }
        int CurrentTrackIndex { get; }
        AudioTrack CurrentTrack { get; set; }
        Task SavePlaylistToLocalStorage();
        Task LoadPlaylistFromLocalStorage();
        string GetAudioTrackName(string episodeName, int partNumber);
    }
}