using System;

namespace ZG
{
    public static class DateTimeUtility
    {
        public static readonly DateTime Utc1970 = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static long GetTicks(uint seconds)
        {
            if (seconds == 0)
                return 0;
            
            return seconds * TimeSpan.TicksPerSecond + Utc1970.Ticks;
        }
        
        public static uint GetSeconds(long ticks)
        {
            return (uint)((ticks - Utc1970.Ticks) / TimeSpan.TicksPerSecond);
        }

        public static uint GetSeconds()
        {
            return GetSeconds(DateTime.UtcNow.Ticks);
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
                return (totalDays > 0.0f) ^ (dayOfWeek >= nowDayOfWeek);
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