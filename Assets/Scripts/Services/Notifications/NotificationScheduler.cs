using Unity.Notifications.Android;
using UnityEngine;
using System;
using LocalCalendar.Data;
using LocalCalendar.Services;

namespace LocalCalendar.Notifications
{
    public enum ReminderTiming
    {
        Invalid,        // not a reminder / missing data
        Past,           // already happened, ignore
        Late,           // recently missed, fire immediately
        Future          // valid future reminder
    }

    public static class NotificationScheduler
    {
        public const int EarlyFireBufferMinutes = 2;
        private const int UpcomingOccurrencesToSchedule = 20;

        public static ReminderTiming GetReminderTiming(CalendarItem item)
        {
            if (item.Type != CalendarItemType.Reminder || item.Reminder == null)
                return ReminderTiming.Invalid;

            DateTime nowUtc = DateTime.UtcNow;
            DateTime intendedUtc = item.StartUtc + item.Reminder.Offset;

            // Past (too old — never fire)
            if (intendedUtc <= nowUtc.AddMinutes(-10))
                return ReminderTiming.Past;

            // Late but still relevant
            if (intendedUtc <= nowUtc)
                return ReminderTiming.Late;

            // Future
            return ReminderTiming.Future;
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

            foreach (var occurrence in RecurrenceExpander.GetUpcomingOccurrences(item, UpcomingOccurrencesToSchedule))
            {
                ScheduleSingleNotification(item, occurrence);
            }
        }

        public static void FireImmediate(CalendarItem item)
        {
            NotificationLogger.Log(new NotificationLogEntry
            {
                ItemId = item.Id,
                Title = item.Title,
                IntendedUtc = item.StartUtc + item.Reminder.Offset,
                ScheduledLocal = item.StartUtc.ToLocalTime(),
                Note = "Immediately firing late notification"
            });

            // Create a startUtc for ScheduleSingleNotification
            DateTime fakeStartUtc = DateTime.UtcNow - item.Reminder.Offset
                + TimeSpan.FromMinutes(EarlyFireBufferMinutes)
                + TimeSpan.FromSeconds(2);
            ScheduleSingleNotification(item, fakeStartUtc);
        }

        private static void ScheduleSingleNotification(CalendarItem item, DateTime startUtc)
        {
            // Exact time commented out. Since Android does not guarantee exact delivery time
            // for local notifcations we schedule notifications slightly beforehand.
            /*DateTime nextUtc = RecurrenceExpander
                .ExpandOccurrences(item, DateTime.UtcNow, DateTime.UtcNow.AddYears(1))
                .FirstOrDefault().ToUniversalTime();*/
            DateTime fireTimeUtc = ComputeFireTimeUtc(item, startUtc);
            // Do not fire notifications for previous times
            //if (fireTimeUtc <= DateTime.UtcNow) fireTimeUtc = DateTime.UtcNow.AddSeconds(5);
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
                ShouldAutoCancel = true,
                IntentData = $"open:item:{item.Id}",
                SmallIcon = "icon_small",
                LargeIcon = "icon_large"
            };

            int notificationId = GetNotificationId(item, fireTimeUtc);
            AndroidNotificationCenter.SendNotificationWithExplicitID(
                notification,
                NotificationInitializer.Channel,
                notificationId
            );

            NotificationLogger.Log(new NotificationLogEntry
            {
                ItemId = item.Id,
                NotificationId = notificationId,
                Title = item.Title,
                IntendedUtc = item.StartUtc + item.Reminder.Offset,
                ScheduledLocal = fireTimeLocal,
                Note = "Scheduled"
            });
        }

        public static void Cancel(CalendarItem item)
        {
            if (item.Type != CalendarItemType.Reminder || item.Reminder == null)
                return;

            if (item.RepeatRule == null)
            {
                CancelSingleNotification(item, item.StartUtc);
                return;
            }

            foreach (var occurrence in RecurrenceExpander.GetUpcomingOccurrences(item, UpcomingOccurrencesToSchedule))
            {
                CancelSingleNotification(item, occurrence);
            }
        }

        private static void CancelSingleNotification(CalendarItem item, DateTime startUtc)
        {
            int notificationId = GetNotificationId(item, ComputeFireTimeUtc(item, startUtc));
            NotificationLogger.Log(new NotificationLogEntry
            {
                ItemId = item.Id,
                NotificationId = notificationId,
                Title = item.Title,
                IntendedUtc = startUtc + item.Reminder.Offset,
                Note = "Cancelled"
            });

            AndroidNotificationCenter.CancelNotification(notificationId);
        }

        public static void CancelAll()
        {
            AndroidNotificationCenter.CancelAllScheduledNotifications();
            AndroidNotificationCenter.CancelAllDisplayedNotifications();
        }

        public static int GetNotificationId(CalendarItem item, DateTime fireTimeUtc)
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + item.Id.GetHashCode();
                hash = hash * 23 + fireTimeUtc.Ticks.GetHashCode();
                return hash;
            }
        }

        private static DateTime ComputeFireTimeUtc(CalendarItem item, DateTime startUtc)
        {
            return startUtc + item.Reminder.Offset - TimeSpan.FromMinutes(EarlyFireBufferMinutes);
        }
    }
}

