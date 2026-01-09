using TMPro;
using UnityEngine;
using System;
using LocalCalendar.Data;
using LocalCalendar.Services;

namespace LocalCalendar.Calendar
{
    public class DayEventRow : MonoBehaviour
    {
        [SerializeField] private TMP_InputField timeLabel;
        [SerializeField] private TMP_InputField titleLabel;
        [SerializeField] private GameObject reminderIcon;
        [SerializeField] private GameObject repeatIcon;
        [SerializeField] private TextMeshProUGUI repeatLabel;

        private CalendarItem _item;
        private Action<CalendarItem> _onClick;

        public void Initialize(CalendarItem item, DateTime occurrenceDay, Action<CalendarItem> onClick)
        {
            _item = item;
            _onClick = onClick;

            // Time

            // Little local function
            DateTime startLocal = item.StartUtc.ToLocalTime();
            DateTime endLocal = item.EndUtc.ToLocalTime();
            bool isFirstDay = occurrenceDay.Date == startLocal.Date;
            bool isLastDay = occurrenceDay.Date == endLocal.Date;
            string timeText;

            if (item.AllDay)
                timeText = "All day";
            else if (isFirstDay && isLastDay) // Single-day event
                timeText = $"{startLocal:hh:mm tt} – {endLocal:hh:mm tt}";
            else if (isFirstDay) // First day of multi-day event
                timeText = $"{startLocal:hh:mm tt} →";
            else if (isLastDay)// Last day of multi-day event
                timeText = $"→ {endLocal:hh:mm tt}";
            else // Middle day of multi-day event
                timeText = "All day";

            timeLabel.text = timeText;
            //timeLabel.text = item.AllDay ? "All day" : item.StartUtc.ToLocalTime().ToString("HH:mm") + (isPm ? "PM" : "AM");

            // Title
            titleLabel.text = item.Title;
            
            Debug.Log((int)_item.Type);
            // Reminder
            reminderIcon.SetActive(_item.Type == CalendarItemType.Reminder);

            // Repeat
            repeatIcon.SetActive(_item.RepeatRule != null);
            repeatLabel.text = item.RepeatRule != null
                ? RepeatFormatter.ToReadableText(item.RepeatRule)
                : "";
            repeatLabel.gameObject.SetActive(item.RepeatRule != null);
        }

        public void OnClick()
        {
            Debug.Log("Clicked item: " + _item.Title);
            // open EditItemScene
            _onClick?.Invoke(_item);
        }
    }
}
