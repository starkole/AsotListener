namespace AsotListener.Common
{
    using System;

    public static class Constants
    {
        /// <summary>
        /// The name of BackgroundUpdater task
        /// </summary>
        public const string BackgroundUpdaterTaskName = "AsotListener.BackgroundUpdater";

        /// <summary>
        /// The default artist name to be used in audio tracks.
        /// </summary>
        public const string DefaultArtist = "Armin van Buuren";

        /// <summary>
        /// The default album artist name to be used in audio tracks.
        /// </summary>
        public const string DefaultAlbumArtist = "Armin van Buuren";

        /// <summary>
        /// Represents unknown duration of audio track
        /// </summary>
        public static readonly TimeSpan UnknownDuration = TimeSpan.MaxValue;
    }
}
