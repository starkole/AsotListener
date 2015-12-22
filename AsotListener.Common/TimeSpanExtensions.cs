namespace AsotListener.Common
{
    using System;

    /// <summary>
    /// Extensions for <see cref="TimeSpan"/>
    /// </summary>
    public static class TimeSpanExtensions
    {
        /// <summary>
        /// Converts <see cref="TimeSpan"/> value to user-friendly string (without milliseconds)
        /// </summary>
        /// <param name="timeSpan"><see cref="TimeSpan"/> to be converted</param>
        /// <returns>User friendly string representation of current <see cref="TimeSpan"/></returns>
        public static string ToUserFriendlyString(this TimeSpan timeSpan)
        {
            int seconds = timeSpan.Seconds + (int)Math.Round(timeSpan.Milliseconds / 1000.0, 0);
            if (timeSpan.Hours >= 1)
            {
                return $"{timeSpan.Hours:D2}:{timeSpan.Minutes:D2}:{seconds:D2}";
            }

            return $"{timeSpan.Minutes:D2}:{seconds:D2}";
        }
    }
}
