using System;

namespace ZG
{
    public static class DateTimeUtility
    {
        public static readonly DateTime Utc1970 = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static uint GetSeconds()
        {
            return (uint)((DateTime.UtcNow.Ticks - Utc1970.Ticks) / TimeSpan.TicksPerSecond);
        }

        public static int GetTotalDays(uint seconds, out DateTime dateTime, out DateTime now)
        {
            dateTime = new DateTime(seconds * TimeSpan.TicksPerSecond + Utc1970.Ticks).ToLocalTime();

            now = DateTime.Now;

            return (int)(now - dateTime).TotalDays;
        }

        public static bool IsToday(uint seconds)
        {
            return Math.Abs(GetTotalDays(seconds, out _, out _)) < 1;
        }

        public static bool IsThisWeek(uint seconds)
        {
            var totalDays = GetTotalDays(seconds, out var dateTime, out var now);
            if (totalDays < 7 && totalDays > -7)
            {
                DayOfWeek dayOfWeek = dateTime.DayOfWeek, nowDayOfWeek = now.DayOfWeek;
                if ((totalDays >= 0.0f) ^ (dayOfWeek >= nowDayOfWeek))
                    return true;
            }

            return false;
        }

        public static bool IsThisMonth(uint seconds)
        {
            var totalDays = GetTotalDays(seconds, out var dateTime, out var now);
            return totalDays < 30 && totalDays > -30 && dateTime.Month == now.Month;
        }
    }
}