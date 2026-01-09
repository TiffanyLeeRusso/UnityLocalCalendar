using System;
using LocalCalendar.Models;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace LocalCalendar.Data
{
    public class CalendarRepository
    {
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

        public List<CalendarItem> GetAllCalendarItems()
        {
            var db = Database.Connection;

            var itemRows = db.Table<CalendarItemRow>().ToList();
            var reminderRows = db.Table<ReminderRow>().ToList();
            var repeatRows = db.Table<RepeatRuleRow>().ToList();

            var reminderMap = reminderRows.ToDictionary(r => r.ItemId);
            var repeatMap = repeatRows.ToDictionary(r => r.ItemId);

            var result = new List<CalendarItem>();

            foreach (var row in itemRows)
            {
                reminderMap.TryGetValue(row.Id, out var reminderRow);
                repeatMap.TryGetValue(row.Id, out var repeatRow);

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

                result.Add(new CalendarItem
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
                });
            }

            return result;
        }

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

        public List<CalendarItem> GetItemsForDay(DateTime localDate)
        {
            DateTime dayStartLocal = localDate.Date;
            DateTime dayEndLocal = dayStartLocal.AddDays(1);

            DateTime startUtc = dayStartLocal.ToUniversalTime();
            DateTime endUtc = dayEndLocal.ToUniversalTime();

            var rows = Database.Connection.Table<CalendarItemRow>()
                .Where(r =>
                       r.StartUtcTicks < endUtc.Ticks &&
                       r.EndUtcTicks >= startUtc.Ticks)
                .ToList();

            var result = new List<CalendarItem>();

            foreach (var row in rows)
            {
                result.Add(new CalendarItem
                {
                    Id = row.Id,
                    Type = (CalendarItemType)row.Type,
                    Title = row.Title,
                    Note = row.Note,
                    StartUtc = new DateTime(row.StartUtcTicks, DateTimeKind.Utc),
                    EndUtc = new DateTime(row.EndUtcTicks, DateTimeKind.Utc),
                    AllDay = row.AllDay == 1
                });
            }

            return result
                .OrderBy(i => i.AllDay ? DateTime.MinValue : i.StartUtc)
                .ToList();
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

    }
}
