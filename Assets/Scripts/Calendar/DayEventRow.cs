using TMPro;
using UnityEngine;
using System;
using LocalCalendar.Data;

namespace LocalCalendar.Calendar
{
    public class DayEventRow : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI timeLabel;
        [SerializeField] private TextMeshProUGUI titleLabel;
        [SerializeField] private GameObject reminderIcon;

        private CalendarItem _item;
        private Action<CalendarItem> _onClick;

        public void Initialize(CalendarItem item, Action<CalendarItem> onClick)
        {
            _item = item;
            _onClick = onClick;

            timeLabel.text = item.AllDay
                ? "All day"
                : item.StartUtc.ToLocalTime().ToString("HH:mm");

            titleLabel.text = item.Title;
            Debug.Log((int)_item.Type);
            reminderIcon.SetActive(_item.Type == CalendarItemType.Reminder);
        }

        public void OnClick()
        {
            Debug.Log("Clicked item: " + _item.Title);
            // open EditItemScene
            _onClick?.Invoke(_item);
        }
    }
}
