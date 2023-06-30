
using System;

namespace Extensions
{
    public enum UnixTimeConvert
    {
        Milliseconds,
        Seconds,
        Minutes,
    }

    public static class TimeExtensions
    {
        public static readonly DateTime UNIX_EPOCH = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static TimeSpan To(this DateTimeOffset from, DateTimeOffset to)
        {
            return to - from;
        }

        /// <summary> 指定されたTimeSpanより短いか. </summary>
        public static bool IsShorterThan(this TimeSpan timeSpan, TimeSpan amount)
        {
            return timeSpan > amount;
        }

        /// <summary> 指定されたTimeSpanより長いか. </summary>
        public static bool IsLongerThan(this TimeSpan timeSpan, TimeSpan amount)
        {
            return timeSpan < amount;
        }

        /// <summary> DateTimeからUnixTimeに変換. </summary>
        public static ulong ToUnixTime(this DateTime dateTime, UnixTimeConvert type = UnixTimeConvert.Milliseconds)
        {
            ulong unixTime = 0;

            var timeSpan = dateTime.ToUniversalTime().Subtract(UNIX_EPOCH);

            switch (type)
            {
                case UnixTimeConvert.Milliseconds:
                    unixTime = (ulong)timeSpan.TotalMilliseconds;
                    break;
                case UnixTimeConvert.Seconds:
                    unixTime = (ulong)timeSpan.TotalSeconds;
                    break;
                case UnixTimeConvert.Minutes:
                    unixTime = (ulong)timeSpan.TotalMinutes;
                    break;
            }

            return unixTime;
        }

        /// <summary> UNIX時間からDateTimeに変換. </summary>
        public static DateTime UnixTimeToDateTime(this long unixTime, UnixTimeConvert type = UnixTimeConvert.Milliseconds)
        {
            return UnixTimeToDateTime((ulong)unixTime, type);
        }

        /// <summary> UNIX時間からDateTimeに変換. </summary>
        public static DateTime UnixTimeToDateTime(this ulong unixTime, UnixTimeConvert type = UnixTimeConvert.Milliseconds)
        {
            var dateTime = DateTime.MinValue;

            switch (type)
            {
                case UnixTimeConvert.Milliseconds:
                    dateTime = UNIX_EPOCH.AddMilliseconds(unixTime);
                    break;
                case UnixTimeConvert.Seconds:
                    dateTime = UNIX_EPOCH.AddSeconds(unixTime);
                    break;
                case UnixTimeConvert.Minutes:
                    dateTime = UNIX_EPOCH.AddMinutes(unixTime);
                    break;
            }

            return dateTime;
        }
    }
}
