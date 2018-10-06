
using System;

namespace Extensions
{
    public static class TimeExtensions
    {
        private static readonly DateTime UNIX_EPOCH = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

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

        /// <summary> 現在時刻からUnixTime (Milliseconds)を計算. </summary>
        public static long ToUnixTime(this DateTime dateTime)
        {
            return (long)dateTime.ToUniversalTime().Subtract(UNIX_EPOCH).TotalMilliseconds;
        }

        /// <summary> UNIX時間からDateTimeに変換. </summary>
        public static DateTime UnixTimeToDateTime(this long unixTime)
        {
            return UNIX_EPOCH.AddMilliseconds(unixTime);
        }
    }
}
