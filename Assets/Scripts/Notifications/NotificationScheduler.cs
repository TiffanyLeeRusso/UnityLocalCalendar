using Unity.Notifications.Android;
using UnityEngine;
using LocalCalendar.Models;
using LocalCalendar.Data;
using LocalCalendar.Services;
using System;

namespace LocalCalendar.Notifications
{
    public static class NotificationScheduler
    {
        public static bool isCurrentReminder(CalendarItem item, bool includeRecentPast = false)
        {
            if (item.Type != CalendarItemType.Reminder || item.Reminder == null)
                return false;

            DateTime nowUtc = DateTime.UtcNow;
            DateTime intendedUtc = item.StartUtc + item.Reminder.Offset;

            // Missed but still relevant (within last 10 min)
            if (includeRecentPast &&
                intendedUtc < nowUtc &&
                intendedUtc > nowUtc.AddMinutes(-10))
            {
                return true;
            }

            // Never schedule notifications from the past
            if (intendedUtc <= nowUtc)
                return false;

            return true;
        }

        public static void Schedule(CalendarItem item)
        {
            if (item.Type != CalendarItemType.Reminder || item.Reminder == null)
                return;

            Cancel(item);

            if (item.RepeatRule == null)
            {
                ScheduleSingleNotification(item, item.StartUtc);
                return;
            }

            foreach (var occurrence in RecurrenceService.GetUpcomingOccurrences(item, 20))
            {
                ScheduleSingleNotification(item, occurrence);
            }
        }

        private static void ScheduleSingleNotification(CalendarItem item, DateTime startUtc)
        {
            // Exact time commented out. Since Android does not guarantee exact delivery time
            // for local notifcations we schedule notifications slightly beforehand.
            /*DateTime nextUtc = RecurrenceExpander
                .ExpandOccurrences(item, DateTime.UtcNow, DateTime.UtcNow.AddYears(1))
                .FirstOrDefault()
                .ToUniversalTime();*/
            DateTime fireTimeUtc =
                item.StartUtc
                + item.Reminder.Offset
                - TimeSpan.FromMinutes(NotificationSettings.EarlyFireBufferMinutes);
            if (fireTimeUtc <= DateTime.UtcNow)
                fireTimeUtc = DateTime.UtcNow.AddSeconds(5);
            DateTime fireTimeLocal = fireTimeUtc.ToLocalTime();

            if (fireTimeLocal <= DateTime.Now)
                return; // don't schedule past notifications

            var notification = new AndroidNotification
            {
                Title = item.Title,
                Text = string.IsNullOrEmpty(item.Note)
                    ? "Reminder"
                    : item.Note,
                FireTime = fireTimeLocal,
                IntentData = $"open:item:{item.Id}",
                SmallIcon = "icon_small",
                LargeIcon = "icon_large"
            };

            int id = AndroidNotificationCenter.SendNotification(
                notification,
                NotificationInitializer.Channel
            );

            // Save notification ID for later cancel/update
            item.NotificationId = id;

            NotificationLogger.Log(new NotificationLogEntry
            {
                ItemId = item.Id,
                Title = item.Title,
                IntendedUtc = item.StartUtc + item.Reminder.Offset,
                ScheduledLocal = fireTimeLocal,
                Note = "Scheduled"
            });
        }

        public static void Cancel(CalendarItem item)
        {
            if (item.NotificationId.HasValue)
            {
                AndroidNotificationCenter.CancelNotification(
                    item.NotificationId.Value);
            }
        }
    }
}

