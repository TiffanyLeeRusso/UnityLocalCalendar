using SQLite;

namespace LocalCalendar.Data
{
    [Table("RepeatRules")]
    public class RepeatRuleRow
    {
        [PrimaryKey]
        public string ItemId { get; set; }

        public int Interval { get; set; }
        public int Unit { get; set; }
        public long? UntilUtcTicks { get; set; }
    }
}
