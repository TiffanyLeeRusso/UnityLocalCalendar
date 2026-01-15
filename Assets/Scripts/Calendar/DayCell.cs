using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using LocalCalendar.Data;

namespace LocalCalendar.Calendar
{
    public class DayCell : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI dayNumber;
        [SerializeField] private Image background;
        [SerializeField] private Transform eventsContainer;
        [SerializeField] private DayEventRow dayEventRowPrefab;

        private DateTime _date;
        private Action<DateTime> _onDayClicked;
        private Action<CalendarItem> _onItemClicked;

        public void Initialize(
            DateTime date,
            bool isToday,
            bool isCurrentMonth,
            List<CalendarItem> items,
            Action<DateTime> onDayClicked,
            Action<CalendarItem> onItemClicked)
        {
            _date = date;
            _onDayClicked = onDayClicked;
            _onItemClicked = onItemClicked;

            dayNumber.text = date.Day.ToString();
            dayNumber.color = isCurrentMonth ? Color.white : Color.gray;

            background.color = isToday
                ? new Color(0.5f, 0.3f, 0.9f)
                : Color.clear;

            BuildRows(items);
        }

        private void BuildRows(List<CalendarItem> items)
        {
            foreach (Transform child in eventsContainer)
                Destroy(child.gameObject);

            foreach (var item in items)
            {
                var row = Instantiate(dayEventRowPrefab, eventsContainer);
                row.Initialize(item, _date, _onItemClicked);
            }
        }

        // Called by Button / EventTrigger on the cell background
        public void OnDayClicked()
        {
            _onDayClicked?.Invoke(_date);
        }
    }
}
