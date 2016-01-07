namespace AsotListener.Services.Contracts
{
    using Models.Enums;
    using Windows.UI.Xaml.Controls;

    public interface IPlaybackManager
    {
        void GoToNextTrack();
        void GoToPreviousTrack();
        void Pause();
        void SchedulePause();
        void Play();
        void UpdateProgressFromSlider(Slider slider);
        void Navigate(int howMany, NavigationInterval interval);
    }
}