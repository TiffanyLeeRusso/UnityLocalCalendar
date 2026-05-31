using SQLite;

namespace LocalCalendar.Models
{
    [Table("CalendarItems")]
    public class CalendarItemRow
    {
        [PrimaryKey]
        public string Id { get; set; }

        public int Type { get; set; }

        public string Title { get; set; }
        public string Note { get; set; }

        public long StartUtcTicks { get; set; }
        public long EndUtcTicks { get; set; }

        public int AllDay { get; set; }

        public int Color { get; set; }
    }
}
