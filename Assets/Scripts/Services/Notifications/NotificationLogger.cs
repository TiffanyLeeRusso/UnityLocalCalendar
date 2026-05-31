using UnityEngine;
using LocalCalendar.Services;
using System.Text;

namespace LocalCalendar.Notifications
{
    public static class NotificationLogger
    {
        public static void Log(NotificationLogEntry entry)
        {
            LoggingService.Info(LogCategory.Notification,
                                DumpToString(entry));
        }

        public static string DumpToString(NotificationLogEntry e)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"Title: {e.Title}");

            if (!string.IsNullOrEmpty(e.ItemId))
                sb.AppendLine($"ItemId: {e.ItemId}");

            sb.AppendLine($"NotificationId: {e.NotificationId}");
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

            return sb.ToString();
        }
    }
}
