using System;

namespace LocalCalendar.Data
{
    [Serializable]
    public class ReminderSettings
    {
        public TimeSpan Offset;

        public ReminderSettings Clone()
        {
            return new ReminderSettings
            {
                Offset = Offset
            };
        }
    }
}
