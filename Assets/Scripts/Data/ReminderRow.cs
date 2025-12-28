using SQLite;

namespace LocalCalendar.Data
{
    [Table("Reminders")]
    public class ReminderRow
    {
        [PrimaryKey]
        public string ItemId { get; set; }

        public int OffsetSeconds { get; set; }
    }
}
