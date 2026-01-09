using System;
using System.Collections.Generic;
using LocalCalendar.Data;
using LocalCalendar.Models;

namespace LocalCalendar.Services
{
    public static class RecurrenceService
    {
        public static IEnumerable<DateTime> ExpandOccurrences(
            CalendarItem item,
            DateTime rangeStartUtc,
            DateTime rangeEndUtc)
        {
            DateTime current = item.StartUtc;

            if (item.RepeatRule == null)
            {
                if (current >= rangeStartUtc && current <= rangeEndUtc)
                    yield return current;
                yield break;
            }

            while (current <= rangeEndUtc)
            {
                if (current >= rangeStartUtc)
                    yield return current;

                current = item.RepeatRule.Unit switch
                {
                    RepeatUnit.Day => current.AddDays(item.RepeatRule.Interval),
                    RepeatUnit.Week => current.AddDays(7 * item.RepeatRule.Interval),
                    RepeatUnit.Month => current.AddMonths(item.RepeatRule.Interval),
                    RepeatUnit.Year => current.AddYears(item.RepeatRule.Interval),
                    _ => current
                };

                if (item.RepeatRule.UntilUtc.HasValue &&
                    current > item.RepeatRule.UntilUtc.Value)
                    break;
            }
        }

        public static IEnumerable<DateTime> GetUpcomingOccurrences(
            CalendarItem item,
            int maxCount)
        {
            DateTime next = item.StartUtc;
            int count = 0;

            while (count < maxCount)
            {
                if (item.RepeatRule.UntilUtc.HasValue &&
                    next > item.RepeatRule.UntilUtc.Value)
                    yield break;

                if (next > DateTime.UtcNow)
                    yield return next;

                next = AddInterval(next, item.RepeatRule);
                count++;
            }
        }

        static DateTime AddInterval(DateTime dt, RepeatRule rule)
        {
            return rule.Unit switch
            {
                RepeatUnit.Day => dt.AddDays(rule.Interval),
                    RepeatUnit.Week => dt.AddDays(7 * rule.Interval),
                    RepeatUnit.Month => dt.AddMonths(rule.Interval),
                    RepeatUnit.Year => dt.AddYears(rule.Interval),
                    _ => dt
                    };
        }
    }
}
