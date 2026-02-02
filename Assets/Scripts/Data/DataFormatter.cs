using System;
using LocalCalendar.Models;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace LocalCalendar.Data
{
    // Functions for printing the DB in human-readable form.
    public static class DataFormatter
    {
        // ReminderSettings
        public static string ToString(ReminderSettings reminder)
        {
            if (reminder == null)
                return "";

            if (reminder.Offset == TimeSpan.Zero)
                return "At time of event";

            if (reminder.Offset.TotalMinutes < 60)
                return $"{(int)reminder.Offset.TotalMinutes} minutes before";

            if (reminder.Offset.TotalHours < 24)
                return $"{(int)reminder.Offset.TotalHours} hours before";

            if (reminder.Offset.TotalDays < 7)
                return $"{(int)reminder.Offset.TotalDays} days before";

            return $"{reminder.Offset.TotalDays:0.#} days before";
        }

        // RepeatRule
        public static string ToString(RepeatRule rule)
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
            string ruleText = $"Every {rule.Interval} {unit}{plural}";

            if (rule.UntilUtc.HasValue)
            {
                ruleText += $" until {rule.UntilUtc.Value.ToLocalTime():MMM d}";
            }

            return ruleText;
        }
    }
}

