using System;

namespace LocalCalendar.Models
{
    public enum RepeatUnit
    {
        Day,
        Week,
        Month,
        Year
    }

    [Serializable]
    public class RepeatRule
    {
        public int Interval;
        public RepeatUnit Unit;
        public DateTime? UntilUtc;
    }
}
