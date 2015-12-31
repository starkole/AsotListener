namespace AsotListener.Services.Contracts
{
    using Windows.UI.Xaml.Controls;

    public interface IPlaybackManager
    {
        void GoToNextTrack();
        void GoToPreviousTrack();
        void Pause();
        void Play();
        void UpdateProgressFromSlider(Slider slider);
    }
}