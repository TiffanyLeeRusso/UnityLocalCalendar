using System;
using System.Collections.Generic;
using LocalCalendar.Data;
using LocalCalendar.Calendar;
using UnityEngine;

namespace LocalCalendar.Services
{
    public static class RecurrenceExpander
    {
        public static IEnumerable<DateTime> ExpandOccurrences(
            CalendarItem item,
            DateTime windowStartLocal,
            DateTime windowEndLocal)
        {
            // Convert base start to local
            DateTime startLocal = item.StartUtc.ToLocalTime();

            // No repeat → single occurrence
            if (item.RepeatRule == null)
            {
                if (startLocal.Date >= windowStartLocal.Date &&
                    startLocal.Date <= windowEndLocal.Date)
                {
                    yield return startLocal;
                }
                yield break;
            }

            var rule = item.RepeatRule;

            DateTime current = startLocal;

            DateTime untilLocal = rule.UntilUtc?.ToLocalTime()
                ?? windowEndLocal;

            // Safety clamp
            if (untilLocal > windowEndLocal)
                untilLocal = windowEndLocal;

            while (current <= untilLocal)
            {
                if (current.Date >= windowStartLocal.Date &&
                    current.Date <= windowEndLocal.Date)
                {
                    yield return current;
                }

                current = AddInterval(current, rule);
            }
        }

        static DateTime AddInterval(DateTime date, RepeatRule rule)
        {
            return rule.Unit switch
            {
                RepeatUnit.Day => date.AddDays(rule.Interval),
                RepeatUnit.Week => date.AddDays(7 * rule.Interval),
                RepeatUnit.Month => date.AddMonths(rule.Interval),
                RepeatUnit.Year => date.AddYears(rule.Interval),
                _ => date
            };
        }
    }
}
