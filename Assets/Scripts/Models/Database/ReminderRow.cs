using SQLite;

namespace LocalCalendar.Models
{
    [Table("Reminders")]
    public class ReminderRow
    {
        [PrimaryKey]
        public string ItemId { get; set; }

        public int OffsetSeconds { get; set; }
    }
}
