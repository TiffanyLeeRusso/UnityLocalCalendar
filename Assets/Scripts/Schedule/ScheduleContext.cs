using System;

namespace LocalCalendar.Schedule
{
    public static class ScheduleContext
    {
        public static DateTime? InitialMonth;

        public static void Clear()
        {
            InitialMonth = null;
        }
    }
}
