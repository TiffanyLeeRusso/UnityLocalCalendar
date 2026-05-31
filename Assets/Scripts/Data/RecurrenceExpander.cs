using System;
using System.Collections.Generic;
using UnityEngine;
using LocalCalendar.Data;
using LocalCalendar.Services;

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
            if (startLocal < new DateTime(2000, 1, 1) || startLocal > new DateTime(2100, 1, 1))
            {
                LoggingService.Error(LogCategory.DB, 
                                     $"ExpandOccurrences: suspicious StartUtc {item.StartUtc} for item {item.Id}; skipping");
                                     yield break;
            }

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
            // Fast-forward to near the window start rather than iterating from startLocal
            // which could be years/decades in the past
            if (currentDate < windowStartLocal.Date)
            {
                currentDate = FastForward(currentDate, windowStartLocal.Date, rule);
            }
            
            DateTime untilLocal = rule.UntilUtc?.ToLocalTime()
                ?? windowEndLocal;

            if (untilLocal > windowEndLocal)
                untilLocal = windowEndLocal;

            DateTime previousDate = currentDate.AddDays(-1); // guaranteed different
            while (true)
            {
                if (currentDate <= previousDate)
                {
                    // AddInterval failed to advance — bail out
                    LoggingService.Error(LogCategory.DB, 
                                         $"ExpandOccurrences: interval did not advance for item {item.Id}, breaking");
                    break;
                }
                previousDate = currentDate;

                DateTime occurrence = currentDate + timeOfDay;
                if (occurrence > untilLocal) break;
                if (occurrence >= windowStartLocal && occurrence <= windowEndLocal)
                    yield return occurrence;

                currentDate = AddInterval(currentDate, rule);
            }
        }

        public static IEnumerable<DateTime> GetUpcomingOccurrences(
            CalendarItem item,
            int maxCount)
        {
            if (item.RepeatRule == null) yield break;

            DateTime next = item.StartUtc;
            int count = 0;

            // Fast-forward to first future occurrence without burning maxCount
            while (next <= DateTime.UtcNow)
            {
                next = AddInterval(next, item.RepeatRule);
                if (item.RepeatRule.UntilUtc.HasValue && next > item.RepeatRule.UntilUtc.Value)
                    yield break;
            }

            while (count < maxCount)
            {
                if (item.RepeatRule.UntilUtc.HasValue && next > item.RepeatRule.UntilUtc.Value)
                    yield break;
                yield return next;
                next = AddInterval(next, item.RepeatRule);
                count++;
            }
        }

        static DateTime AddInterval(DateTime date, RepeatRule rule)
        {
            return rule.Unit switch
            {
                RepeatUnit.Day   => date.AddDays(rule.Interval),
                    RepeatUnit.Week  => date.AddDays(7 * rule.Interval),
                    RepeatUnit.Month => date.AddMonths(rule.Interval),
                    RepeatUnit.Year  => date.AddYears(rule.Interval),
                    _                => date.AddDays(1) // safe fallback; prevent infinite looping
                    };
        }

        static DateTime FastForward(DateTime from, DateTime target, RepeatRule rule)
        {
            if (rule.Interval <= 0) return target; // safety

            // Estimate how many intervals to skip
            double days = (target - from).TotalDays;
            double intervalDays = rule.Unit switch
                {
                    RepeatUnit.Day   => rule.Interval,
                    RepeatUnit.Week  => rule.Interval * 7,
                    RepeatUnit.Month => rule.Interval * 30.436875, // average
                    RepeatUnit.Year  => rule.Interval * 365.2425,
                    _                => 1
                };

            int skip = Math.Max(0, (int)(days / intervalDays) - 1); // -1 to not overshoot
            DateTime result = from;
    
            // Apply bulk skip
            result = rule.Unit switch
                {
                    RepeatUnit.Day   => result.AddDays(skip * rule.Interval),
                    RepeatUnit.Week  => result.AddDays(skip * rule.Interval * 7),
                    RepeatUnit.Month => result.AddMonths(skip * rule.Interval),
                    RepeatUnit.Year  => result.AddYears(skip * rule.Interval),
                    _                => result.AddDays(skip)
                };

            return result;
        }
    }
}
