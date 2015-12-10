namespace AsotListener.Models
{
    using Attributes;

    public enum EpisodeStatus
    {
        [LocalizedDisplay("")]
        CanBeLoaded,

        [LocalizedDisplay("Downloading", "EpisodeStatus.Downloading")]
        Downloading,

        [LocalizedDisplay("Loaded", "EpisodeStatus.Loaded")]
        Loaded,

        [LocalizedDisplay("Playing", "EpisodeStatus.Playing")]
        Playing
    }
}
