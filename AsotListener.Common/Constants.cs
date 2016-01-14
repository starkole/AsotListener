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
        /// Represents unknown duration of audio track
        /// </summary>
        public static readonly TimeSpan UnknownDuration = TimeSpan.MaxValue;
    }
}
