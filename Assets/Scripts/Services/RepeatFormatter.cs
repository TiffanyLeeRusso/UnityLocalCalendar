using LocalCalendar.Data;

namespace LocalCalendar.Services
{
    public static class RepeatFormatter
    {
        public static string ToReadableText(RepeatRule rule)
        {
            if (rule == null)
                return string.Empty;

            string unit = rule.Unit switch
            {
                RepeatUnit.Day => "day",
                RepeatUnit.Week => "week",
                RepeatUnit.Month => "month",
                RepeatUnit.Year => "year",
                _ => ""
            };

            string plural = rule.Interval > 1 ? "s" : "";

            string text = $"Every {rule.Interval} {unit}{plural}";

            if (rule.UntilUtc.HasValue)
            {
                text += $" until {rule.UntilUtc.Value.ToLocalTime():MMM d}";
            }

            return text;
        }
    }
}
