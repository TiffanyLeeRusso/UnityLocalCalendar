using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using LocalCalendar.Data;

namespace LocalCalendar.Calendar
{
    public static class CalendarUtils
    {
        const int DAYS_IN_GRID = 42;
        const int GRID_COLUMNS = 7;

        public static int DaysInMonth(int year, int month)
            => DateTime.DaysInMonth(year, month);

        public static int FirstWeekdayOfMonth(int year, int month)
            => (int)new DateTime(year, month, 1).DayOfWeek; // 0 = Sunday

        // GetExpandedMonthItems
        public static IOrderedEnumerable<IGrouping<DateTime, (DateTime date, CalendarItem item, DateTime occurrenceStart)>>
            GetExpandedMonthItems(CalendarRepository repo, DateTime rangeStart, DateTime rangeEnd)
        {
            var items = repo.GetItemsForMonth(rangeStart);
            var expanded = new List<(DateTime date, CalendarItem item, DateTime occurrenceStart)>();

            foreach (var item in items)
            {
                foreach (DateTime occurrenceStartLocal in
                         RecurrenceExpander.ExpandOccurrences(item, rangeStart, rangeEnd))
                {
                    DateTime occurrenceEndLocal = CalendarUtils.GetOccurrenceEnd(item, occurrenceStartLocal);
                    DateTime dayCursor = occurrenceStartLocal.Date;
                    DateTime lastDay = occurrenceEndLocal.Date;

                    while (dayCursor <= lastDay)
                    {
                        if (dayCursor >= rangeStart && dayCursor <= rangeEnd)
                        {
                            DateTime dayStart = dayCursor;
                            DateTime dayEnd = dayStart.AddDays(1);

                            bool overlaps = occurrenceStartLocal < dayEnd &&
                                occurrenceEndLocal > dayStart;

                            if (overlaps)
                                expanded.Add((dayCursor, item, occurrenceStartLocal));
                        }

                        dayCursor = dayCursor.AddDays(1);
                    }
                }
            }

            return expanded
                .OrderBy(e => e.date)
                .ThenBy(e => e.item.AllDay ? 0 : 1) // All-day items first
                .ThenBy(e => e.occurrenceStart)
                .GroupBy(e => e.date)
                .OrderBy(g => g.Key);
        }

        // GetExpandedDayItems
        public static List<(CalendarItem item, DateTime occurrenceStart)> GetExpandedDayItems(CalendarRepository repo, DateTime localDate)
        {
            DateTime dayStart = localDate.Date;
            DateTime dayEnd = dayStart.AddDays(1);

            var candidates = repo.GetItemsForDay(localDate);
            var result = new List<(CalendarItem item, DateTime occurrenceStart)>();

            foreach (var item in candidates)
            {
                // Non-repeating: simple overlap
                if (item.RepeatRule == null)
                {
                    DateTime startLocal = item.StartUtc.ToLocalTime();
                    DateTime endLocal = CalendarUtils.GetOccurrenceEnd(item, startLocal);

                    if (startLocal < dayEnd && endLocal > dayStart)
                        result.Add((item, startLocal));

                    continue;
                }

                // Repeating: must expand far enough back
                TimeSpan duration = item.EndUtc > item.StartUtc
                    ? item.EndUtc - item.StartUtc
                    : TimeSpan.FromMinutes(1);

                DateTime expandStart = dayStart - duration;

                foreach (DateTime occurrenceStartLocal in
                         RecurrenceExpander.ExpandOccurrences(item, expandStart, dayEnd))
                {
                    DateTime occurrenceEndLocal =
                        CalendarUtils.GetOccurrenceEnd(item, occurrenceStartLocal);

                    if (occurrenceStartLocal < dayEnd && occurrenceEndLocal > dayStart)
                    {
                        result.Add((item, occurrenceStartLocal));
                        break;
                    }
                }
            }

            return result
                .OrderBy(r => r.item.AllDay ? 0 : 1)
                .ThenBy(r => r.occurrenceStart)
                .ThenBy(r => r.item.Title)
                .ToList();
        }

        // GetOccurrenceEnd
        public static DateTime GetOccurrenceEnd(CalendarItem item, DateTime occurrenceStart)
        {
            if (item.EndUtc > item.StartUtc)
            {
                TimeSpan duration = item.EndUtc - item.StartUtc;
                return occurrenceStart.Add(duration);
            }
            else
            {
                return occurrenceStart.AddMinutes(1);
            }
        }

        // ResizeGrid
        public static Vector2 ResizeGrid(GridLayoutGroup grid, RectTransform monthGrid)
        {
            int rows = Mathf.CeilToInt(DAYS_IN_GRID / (float)GRID_COLUMNS); // 6

            LayoutRebuilder.ForceRebuildLayoutImmediate(monthGrid);
            float totalWidth  = monthGrid.rect.width;
            float totalHeight = monthGrid.rect.height;

            float usableWidth =
                totalWidth
                - grid.padding.left
                - grid.padding.right
                - grid.spacing.x * (GRID_COLUMNS - 1);

            float usableHeight =
                totalHeight
                - grid.padding.top
                - grid.padding.bottom
                - grid.spacing.y * (rows - 1);

            float cellWidth  = usableWidth / GRID_COLUMNS;
            float cellHeight = usableHeight / rows;

            return new Vector2(cellWidth, cellHeight);
        }
    }
}
