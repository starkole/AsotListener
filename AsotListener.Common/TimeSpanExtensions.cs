namespace AsotListener.Common
{
    using System;

    public static class TimeSpanExtensions
    {
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
