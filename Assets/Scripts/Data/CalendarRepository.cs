using System;
using LocalCalendar.Models;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using LocalCalendar.Services;

namespace LocalCalendar.Data
{
    public class CalendarRepository
    {
        // --- DB Modifiers ---

        public void Save(CalendarItem item)
        {
            var db = Database.Connection;

            db.RunInTransaction(() =>
            {
                db.InsertOrReplace(new CalendarItemRow
                {
                    Id = item.Id,
                    Type = (int)item.Type,
                    Title = item.Title,
                    Note = item.Note,
                    StartUtcTicks = item.StartUtc.Ticks,
                    EndUtcTicks = item.EndUtc.Ticks,
                    AllDay = item.AllDay ? 1 : 0
                });

                db.Delete<RepeatRuleRow>(item.Id);
                if (item.RepeatRule != null)
                {
                    db.Insert(new RepeatRuleRow
                    {
                        ItemId = item.Id,
                        Interval = item.RepeatRule.Interval,
                        Unit = (int)item.RepeatRule.Unit,
                        UntilUtcTicks = item.RepeatRule.UntilUtc?.Ticks
                    });
                }

                db.Delete<ReminderRow>(item.Id);
                if (item.Reminder != null)
                {
                    db.Insert(new ReminderRow
                    {
                        ItemId = item.Id,
                        OffsetSeconds = (int)item.Reminder.Offset.TotalSeconds
                    });
                }
            });
        }

        public void Delete(string id)
        {
            var db = Database.Connection;

            db.RunInTransaction(() =>
            {
                db.Delete<CalendarItemRow>(id);
                db.Delete<RepeatRuleRow>(id);
                db.Delete<ReminderRow>(id);
            });
        }


        // --- DB Gets ---

        public List<CalendarItem> Search(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return new List<CalendarItem>();

            query = query.ToLowerInvariant();

            return GetAllCalendarItems()
                .Where(x =>
                       (!string.IsNullOrEmpty(x.Title) &&
                        x.Title.ToLowerInvariant().Contains(query)) ||
                       (!string.IsNullOrEmpty(x.Note) &&
                        x.Note.ToLowerInvariant().Contains(query))
                )
                .OrderBy(item => item.StartUtc)
                .ToList();
        }

        private void LoadMaps(
            out Dictionary<string, ReminderRow> reminderMap,
            out Dictionary<string, RepeatRuleRow> repeatMap)
        {
            var db = Database.Connection;

            reminderMap = db.Table<ReminderRow>()
                .ToDictionary(r => r.ItemId);

            repeatMap = db.Table<RepeatRuleRow>()
                .ToDictionary(r => r.ItemId);
        }

        private CalendarItem BuildItem(
            CalendarItemRow row,
            ReminderRow reminderRow,
            RepeatRuleRow repeatRow)
        {
            ReminderSettings reminder = null;
            if (reminderRow != null)
            {
                reminder = new ReminderSettings
                {
                    Offset = TimeSpan.FromSeconds(reminderRow.OffsetSeconds)
                };
            }

            RepeatRule repeatRule = null;
            if (repeatRow != null)
            {
                repeatRule = new RepeatRule
                {
                    Interval = repeatRow.Interval,
                    Unit = (RepeatUnit)repeatRow.Unit,
                    UntilUtc = repeatRow.UntilUtcTicks.HasValue
                    ? new DateTime(
                        repeatRow.UntilUtcTicks.Value,
                        DateTimeKind.Utc)
                        : (DateTime?)null
                };
            }

            return new CalendarItem
            {
                Id = row.Id,
                Type = (CalendarItemType)row.Type,
                Title = row.Title,
                Note = row.Note,
                StartUtc = new DateTime(row.StartUtcTicks, DateTimeKind.Utc),
                EndUtc = new DateTime(row.EndUtcTicks, DateTimeKind.Utc),
                AllDay = row.AllDay == 1,
                Reminder = reminder,
                RepeatRule = repeatRule
            };
        }

        private List<CalendarItem> OrderItems(List<CalendarItem>items)
        {
            return items
                .OrderBy(i => i.AllDay ? DateTime.MinValue : i.StartUtc)
                .ToList();
        }

        public List<CalendarItem> GetAllCalendarItems()
        {
            var db = Database.Connection;

            var itemRows = db.Table<CalendarItemRow>().ToList();
            LoadMaps(out var reminderMap, out var repeatMap);

            var result = new List<CalendarItem>();
            foreach (var row in itemRows)
            {
                reminderMap.TryGetValue(row.Id, out var reminderRow);
                repeatMap.TryGetValue(row.Id, out var repeatRow);

                result.Add(BuildItem(row, reminderRow, repeatRow));
            }

            return result;
        }

        public List<CalendarItem> GetItemsForDay(DateTime localDate)
        {
            var db = Database.Connection;

            DateTime dayStartUtc = localDate.Date.ToUniversalTime();
            DateTime dayEndUtc = localDate.Date.AddDays(1).ToUniversalTime();

            LoadMaps(out var reminderMap, out var repeatMap);

            // Broad candidate fetch
            var rows = db.Table<CalendarItemRow>()
                .Where(r =>
                       // Non-repeating overlap
                       (r.StartUtcTicks < dayEndUtc.Ticks &&
                        r.EndUtcTicks > dayStartUtc.Ticks)
                       ||
                       // Repeating items that started before this day ends
                       r.StartUtcTicks < dayEndUtc.Ticks
                )
                .ToList();

            var result = new List<CalendarItem>();

            foreach (var row in rows)
            {
                repeatMap.TryGetValue(row.Id, out var repeatRow);

                // Recurrence already ended before this day
                if (repeatRow != null &&
                    repeatRow.UntilUtcTicks.HasValue &&
                    repeatRow.UntilUtcTicks.Value < dayStartUtc.Ticks)
                {
                    continue;
                }

                reminderMap.TryGetValue(row.Id, out var reminderRow);
                result.Add(BuildItem(row, reminderRow, repeatRow));
            }

            return OrderItems(result);
        }

        public List<CalendarItem> GetItemsForMonth(DateTime monthLocal)
        {
            var db = Database.Connection;

            DateTime monthStartLocal =
                new DateTime(monthLocal.Year, monthLocal.Month, 1);

            DateTime monthEndLocal =
                monthStartLocal.AddMonths(1);

            DateTime monthStartUtc = monthStartLocal.ToUniversalTime();
            DateTime monthEndUtc = monthEndLocal.ToUniversalTime();

            LoadMaps(out var reminderMap, out var repeatMap);

            var candidateRows = db.Table<CalendarItemRow>()
                .Where(r =>
                       // Non-repeating overlap
                       (r.StartUtcTicks < monthEndUtc.Ticks &&
                        r.EndUtcTicks > monthStartUtc.Ticks)
                       ||
                       // Possibly repeating (start before end of month)
                       r.StartUtcTicks < monthEndUtc.Ticks
                )
                .ToList();

            var result = new List<CalendarItem>();
            foreach (var row in candidateRows)
            {
                repeatMap.TryGetValue(row.Id, out var repeatRow);

                // If repeating, check UntilUtc
                if (repeatRow != null)
                {
                    if (repeatRow.UntilUtcTicks.HasValue &&
                        repeatRow.UntilUtcTicks.Value < monthStartUtc.Ticks)
                    {
                        continue; // recurrence ended before this month
                    }
                }
                else {} // Non-repeating item already filtered by overlap

                reminderMap.TryGetValue(row.Id, out var reminderRow);
                result.Add(BuildItem(row, reminderRow, repeatRow));
            }

            return OrderItems(result);
        }


        public CalendarItem GetById(string id)
        {
            // Calendar Item
            var row = Database.Connection.Find<CalendarItemRow>(id);
            if (row == null) return null;

            var item = new CalendarItem
            {
                Id = row.Id,
                Title = row.Title,
                Note = row.Note,
                Type = (CalendarItemType)row.Type,
                StartUtc = new DateTime(row.StartUtcTicks, DateTimeKind.Utc),
                EndUtc = new DateTime(row.EndUtcTicks, DateTimeKind.Utc),
                AllDay = row.AllDay == 1
            };
            
            // Reminder
            var reminder = Database.Connection.Find<ReminderRow>(id);
            if (reminder != null)
            {
                item.Reminder = new ReminderSettings
                {
                    Offset = TimeSpan.FromSeconds(reminder.OffsetSeconds)
                };
            }

            // Repeat
            var repeat = Database.Connection.Find<RepeatRuleRow>(id);
            if (repeat != null)
            {
                item.RepeatRule = new RepeatRule
                {
                    Interval = repeat.Interval,
                    Unit = (RepeatUnit)repeat.Unit,
                    UntilUtc = repeat.UntilUtcTicks.HasValue
                    ? new DateTime(repeat.UntilUtcTicks.Value, DateTimeKind.Utc)
                    : null
                };
            }

            return item;
        }

        // --- Debug ---

        public List<CalendarItemDebugDump> GetAll()
        {
            var db = Database.Connection;

            var items = db.Table<CalendarItemRow>().ToList();
            var reminders = db.Table<ReminderRow>().ToList();
            var repeats = db.Table<RepeatRuleRow>().ToList();

            var reminderMap = reminders.ToDictionary(r => r.ItemId);
            var repeatMap = repeats.ToDictionary(r => r.ItemId);

            var result = new List<CalendarItemDebugDump>();

            foreach (var row in items)
            {
                var item = new CalendarItem
                {
                    Id = row.Id,
                    Type = (CalendarItemType)row.Type,
                    Title = row.Title,
                    Note = row.Note,
                    StartUtc = new DateTime(row.StartUtcTicks, DateTimeKind.Utc),
                    EndUtc = new DateTime(row.EndUtcTicks, DateTimeKind.Utc),
                    AllDay = row.AllDay == 1
                };

                reminderMap.TryGetValue(row.Id, out var reminder);
                repeatMap.TryGetValue(row.Id, out var repeat);

                result.Add(new CalendarItemDebugDump
                {
                    Item = item,
                    Reminder = reminder,
                    RepeatRule = repeat
                });
            }

            return result;
        }

        // Call GetAll() and pass the result to this function
        public string AllDBToString(List<CalendarItemDebugDump> dump)
        {
            var sb = new StringBuilder();

            sb.AppendLine("=== DATABASE DUMP ===");
            sb.AppendLine($"Items: {dump.Count}");
            sb.AppendLine();

            foreach (var entry in dump)
            {
                var item = entry.Item;

                sb.AppendLine($"ID: {item.Id}");
                sb.AppendLine($"Title: {item.Title}");
                sb.AppendLine($"Type: {item.Type}");
                sb.AppendLine($"Start: {item.StartUtc:u}");
                sb.AppendLine($"End:   {item.EndUtc:u}");
                sb.AppendLine($"AllDay: {item.AllDay}");

                if (entry.Reminder != null)
                {
                    sb.AppendLine(
                        $"Reminder: {TimeSpan.FromSeconds(entry.Reminder.OffsetSeconds)}");
                }
                else
                {
                    sb.AppendLine("Reminder: (none)");
                }

                if (entry.RepeatRule != null)
                {
                    sb.AppendLine(
                        $"Repeat: every {entry.RepeatRule.Interval} " +
                        $"{(RepeatUnit)entry.RepeatRule.Unit}" +
                        (entry.RepeatRule.UntilUtcTicks.HasValue
                         ? $" until {new DateTime(entry.RepeatRule.UntilUtcTicks.Value, DateTimeKind.Utc):u}"
                         : ""));
                }
                else
                {
                    sb.AppendLine("Repeat: (none)");
                }

                sb.AppendLine(new string('-', 40));
            }

            return sb.ToString();
        }
    }
}
