using System.Collections.Generic;
using UnityEngine;
using System.Text;

namespace LocalCalendar.Notifications
{
    public static class NotificationLogger
    {
        private const int MaxEntries = 50;
        private static readonly List<NotificationLogEntry> Entries = new();

        public static void Log(NotificationLogEntry entry)
        {
            if (Entries.Count >= MaxEntries)
                Entries.RemoveAt(0);

            Entries.Add(entry);
        }

        public static IReadOnlyList<NotificationLogEntry> GetEntries()
        {
            return Entries;
        }

        public static void Clear()
        {
            Entries.Clear();
        }

        public static string DumpToString()
        {
            var sb = new StringBuilder();

            sb.AppendLine("=== Notification Diagnostics ===");
            sb.AppendLine($"Entries: {Entries.Count}");
            sb.AppendLine($"Now (local): {System.DateTime.Now}");
            sb.AppendLine();

            foreach (var e in Entries)
            {
                sb.AppendLine("— — — — — — — — — — —");
                sb.AppendLine($"Title: {e.Title}");

                if (!string.IsNullOrEmpty(e.ItemId))
                    sb.AppendLine($"ItemId: {e.ItemId}");

                sb.AppendLine($"Intended (UTC): {e.IntendedUtc}");

                sb.AppendLine($"Scheduled (local): {e.ScheduledLocal}");

                if (e.FiredLocal.HasValue)
                {
                    sb.AppendLine($"Fired (local): {e.FiredLocal.Value}");

                    if (e.IntendedUtc != default)
                    {
                        var drift =
                            e.FiredLocal.Value -
                            e.IntendedUtc.ToLocalTime();

                        sb.AppendLine($"Drift: {drift.TotalMinutes:+0.0;-0.0} min");
                    }
                }
                else
                {
                    sb.AppendLine("Fired: —");
                }

                if (!string.IsNullOrEmpty(e.Note))
                    sb.AppendLine($"Note: {e.Note}");

                sb.AppendLine();
            }

            return sb.ToString();
        }

    }
}
