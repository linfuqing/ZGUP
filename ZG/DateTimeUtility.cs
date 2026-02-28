using System;

namespace ZG
{
    public static class DateTimeUtility
    {
        public enum DataTimeType
        {
            UTC, 
            Local
        }
        
        public static readonly DateTime UTC1970 = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static long GetTicks(uint seconds)
        {
            if (seconds == 0)
                return 0;
            
            return seconds * TimeSpan.TicksPerSecond + UTC1970.Ticks;
        }
        
        public static uint GetSeconds(long ticks)
        {
            return (uint)((ticks - UTC1970.Ticks) / TimeSpan.TicksPerSecond);
        }

        public static uint GetSeconds()
        {
            return GetSeconds(DateTime.UtcNow.Ticks);
        }

        public static int GetTotalDays(uint seconds, out DateTime dateTime, out DateTime now, DataTimeType type)
        {
            switch (type)
            {
                case DataTimeType.Local:
                    dateTime = new DateTime(GetTicks(seconds)).ToLocalTime();

                    now = DateTime.Now;
                    break;
                default:
                    dateTime = new DateTime(GetTicks(seconds));
                    
                    now = DateTime.UtcNow;
                    break;
            }
            
            return (int)(now.Ticks / TimeSpan.TicksPerDay - dateTime.Ticks / TimeSpan.TicksPerDay);
        }

        public static bool IsToday(uint seconds, DataTimeType type)
        {
            return Math.Abs(GetTotalDays(seconds, out _, out _, type)) < 1;
        }

        public static bool IsThisWeek(uint seconds, DataTimeType type)
        {
            int totalDays = GetTotalDays(seconds, out var dateTime, out var now, type);
            if (Math.Abs(totalDays) < 7)
            {
                DayOfWeek dayOfWeek = dateTime.DayOfWeek, nowDayOfWeek = now.DayOfWeek;
                return (totalDays > 0) ^ (dayOfWeek >= nowDayOfWeek);
            }

            return false;
        }

        public static bool IsThisMonth(uint seconds, DataTimeType type)
        {
            int totalDays = GetTotalDays(seconds, out var dateTime, out var now, type);
            return Math.Abs(totalDays) < 30 && dateTime.Month == now.Month;
        }
    }
}