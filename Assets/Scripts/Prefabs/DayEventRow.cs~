using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using LocalCalendar.Data;
using LocalCalendar.Services;

namespace LocalCalendar.Prefabs
{
    public class DayEventRow : MonoBehaviour
    {
        public enum ViewMode
        {
            Full,
            Compact
        }

        [SerializeField] private TextMeshProUGUI titleLabel;
        [SerializeField] private TextMeshProUGUI timeLabel;
        [SerializeField] private GameObject iconContainer;
        [SerializeField] private GameObject reminderIcon;
        [SerializeField] private GameObject repeatIcon;
        [SerializeField] private TextMeshProUGUI repeatLabel;

        private CalendarItem _item;
        private DateTime _shownOnDate;
        private Action<(CalendarItem item, DateTime shownOnDate)> _onClick;

        public void Initialize(CalendarItem item,
                               DateTime occurrenceStart,
                               Action<(CalendarItem item, DateTime shownOnDate)> onClick,
                               DateTime shownOnDate,
                               ViewMode mode = ViewMode.Full)
        {
            _item = item;
            _onClick = onClick;
            _shownOnDate = shownOnDate;

            DateTime occurrenceEnd = CalendarUtils.GetOccurrenceEnd(item, occurrenceStart);
            bool isSingleDay = occurrenceStart.Date == occurrenceEnd.Date;
            bool isFirstDay = shownOnDate.Date == occurrenceStart.Date;
            bool isLastDay  = shownOnDate.Date == occurrenceEnd.Date;

            string timeText;
            if (item.AllDay || 
                (occurrenceStart.TimeOfDay == TimeSpan.Zero &&
                 (occurrenceEnd - occurrenceStart).TotalHours >= 23))
                timeText = "All day";
            else if (isSingleDay)
            {
                string start = AppUtils.FormatTime(occurrenceStart);
                string end   = AppUtils.FormatTime(occurrenceEnd);
                timeText = $"{start} – {end}";
                //timeText = mode == ViewMode.Compact
                //    ? $"{start}\n – \n{end}"
                //    : $"{start} – {end}";
            }
            else
            {
                if (isFirstDay)
                    timeText = $"{occurrenceStart:hh:mm tt} →";
                else if (isLastDay)
                    timeText = $"→ {occurrenceEnd:hh:mm tt}";
                else
                    timeText = "→";
            }
            timeLabel.text = timeText;
            titleLabel.text = item.Title;

            if(mode == ViewMode.Full)
            {
                reminderIcon.SetActive(_item.Type == CalendarItemType.Reminder);

                repeatIcon.SetActive(_item.RepeatRule != null);
                repeatLabel.text = item.RepeatRule != null
                    ? DataFormatter.ToString(item.RepeatRule)
                    : "";
                repeatLabel.gameObject.SetActive(item.RepeatRule != null);
            }
        }

        public void OnClick()
        {
            _onClick?.Invoke((item: _item, shownOnDate: _shownOnDate));
        }
    }
}
