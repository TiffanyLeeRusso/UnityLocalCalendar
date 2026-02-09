using System;
using System.Collections.Generic;
using LocalCalendar.Data;
using UnityEngine;

namespace LocalCalendar.Data
{
    public static class RecurrenceExpander
    {
        public static IEnumerable<DateTime> ExpandOccurrences(
            CalendarItem item,
            DateTime windowStartLocal,
            DateTime windowEndLocal)
        {
            DateTime startLocal = item.StartUtc.ToLocalTime();
            TimeSpan timeOfDay = startLocal.TimeOfDay;

            // Normalize window to full span
            windowStartLocal = windowStartLocal.Date;
            windowEndLocal = windowEndLocal.Date.AddDays(1).AddTicks(-1);

            // No repeat → single occurrence
            if (item.RepeatRule == null)
            {
                if (startLocal >= windowStartLocal &&
                    startLocal <= windowEndLocal)
                {
                    yield return startLocal;
                }
                yield break;
            }

            var rule = item.RepeatRule;

            DateTime currentDate = startLocal.Date;

            DateTime untilLocal = rule.UntilUtc?.ToLocalTime()
                ?? windowEndLocal;

            if (untilLocal > windowEndLocal)
                untilLocal = windowEndLocal;

            while (true)
            {
                DateTime occurrence = currentDate + timeOfDay;

                if (occurrence > untilLocal)
                    break;

                if (occurrence >= windowStartLocal &&
                    occurrence <= windowEndLocal)
                {
                    yield return occurrence;
                }

                currentDate = AddInterval(currentDate, rule);
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
