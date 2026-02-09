using System;
using LocalCalendar.Models;

namespace LocalCalendar.Data
{
    public enum CalendarItemType
    {
        Event = 0,
        Reminder = 1
    }

    [Serializable]
    public class CalendarItem
    {
        public string Id;
        public CalendarItemType Type;

        public string Title;
        public string Note;

        public DateTime StartUtc;
        public DateTime EndUtc;

        public bool AllDay;

        public ReminderSettings Reminder;
        public RepeatRule RepeatRule;

        public CalendarItem Clone()
        {
            return new CalendarItem
            {
                Id = Id,
                Type = Type,
                Title = Title,
                Note = Note,
                StartUtc = StartUtc,
                EndUtc = EndUtc,
                AllDay = AllDay,
                Reminder = Reminder?.Clone(),
                RepeatRule = RepeatRule?.Clone()
            };
        }
    }
}
