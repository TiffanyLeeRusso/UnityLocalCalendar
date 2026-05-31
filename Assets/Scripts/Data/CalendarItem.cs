using System;
using LocalCalendar.Models;
using Newtonsoft.Json;

namespace LocalCalendar.Data
{
    /*
      !!!!!!!!! READ THIS BEFORE CHANGING THIS FILE !!!!!!!!!

      When changing the DB schema, all of these things must
      be updated/done: (tldr; search the code files for "On DB-Schema Change"

      1. CalendarItem / RepeatRule / ReminderSettings data classes (Data/*)
      2. CalendarItemRow / RepeatRuleRow / ReminderRow DB row classes (Models/*)
      3. Database.JsonWrapper.CurrentVersion: Increment and update the comment about the JSON format
      4. Database.RunMigrations(): Update to handle change(s)
      5. Database.ImportFromJsonFile(): Handle backfilling/migrating legacy JSON DB-import files
      6. Database.ValidateWrapper(): Add/update field checks
      7. CalendarRepository: Save/GetAllCalendarItems/GetAll/AllDBToString if mapping changes
      8. Test the import/export as well as the DB migration on both app start and DB import
     */
    public enum CalendarItemType
    {
        Event = 0,
        Reminder = 1
    }

    public enum CalendarItemColor
    {
        Transparent = 0,
        Blue = 1,
        Green = 2,
        Amber = 3,
        Rose = 4
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

        public CalendarItemColor Color;

        public ReminderSettings Reminder;
        public RepeatRule RepeatRule;

        [JsonIgnore]
        public bool IsMultiDay => (EndUtc - StartUtc).TotalDays >= 1.0;
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
                Color = Color,
                Reminder = Reminder?.Clone(),
                RepeatRule = RepeatRule?.Clone()
            };
        }
    }
}
