using System;

namespace LocalCalendar.Notifications
{
    [Serializable]
    public class NotificationLogEntry
    {
        public string ItemId;
        public int NotificationId;
        public string Title;
        public DateTime IntendedUtc;
        public DateTime ScheduledLocal;
        public DateTime? FiredLocal;
        public string Note;
    }
}
