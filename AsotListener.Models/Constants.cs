namespace AsotListener.Models
{
    /// <summary>
    /// Collection of string constants used in the entire solution. This file is shared for all projects
    /// </summary>
    public static class Constants
    {
        public const string LoggingSessionName = "AsotListenerLoggingSession";
        public const string Playlist = "Playlist";
        public const string CurrentTrack = "CurrentTrack";
        public const string EpisodesList = "EpisodesList";

        public const string StartPlayback = "StartPlayback";
        public const string SkipNext = "SkipNext";
        public const string IsBackgroundTaskRunning = "IsBackgroundTaskRunning";
        public const string SkipPrevious = "SkipPrevious";
        public const string PausePlayback = "PausePlayback";

        public const int BackgroundAudioWaitingTime = 2000; // 2 sec.
        public const double DefaultEpisodeSize = 400 * 1024 * 1024; // 400MB
    }
}
