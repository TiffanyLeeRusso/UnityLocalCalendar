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
        [SerializeField] private GameObject highlight;
        [SerializeField] private RectTransform eventsContainer;
        [SerializeField] private DayEventRow dayEventRowPrefab;

        private DateTime _date;
        private Action<DateTime> _onDayClicked;
        private Action<(CalendarItem item, DateTime shownOnDate)> _onItemClicked;
        
        public void Initialize(
            DateTime date,
            bool isToday,
            bool isCurrentMonth,
            List<(CalendarItem item, DateTime occurrenceStart)> items,
            Action<DateTime> onDayClicked,
            Action<(CalendarItem item, DateTime shownOnDate)> onItemClicked)
        {
            _date = date;
            _onDayClicked = onDayClicked;
            _onItemClicked = onItemClicked;

            dayNumber.text = date.Day.ToString();
            dayNumber.color = isCurrentMonth ? Color.white : Color.gray;

            highlight.SetActive(isToday);

            BuildRows(items);
        }

        private void BuildRows(List<(CalendarItem item, DateTime occurrenceStart)> items)
        {
            foreach (Transform child in eventsContainer)
                Destroy(child.gameObject);

            foreach (var item in items)
            {
                var row = Instantiate(dayEventRowPrefab, eventsContainer);
                row.Initialize(item.item, item.occurrenceStart, _onItemClicked, _date, DayEventRow.ViewMode.Compact);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(eventsContainer);
        }

        // Called by Button / EventTrigger on the cell background
        public void OnDayClicked()
        {
            _onDayClicked?.Invoke(_date);
        }
    }
}
