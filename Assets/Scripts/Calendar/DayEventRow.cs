using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;
using LocalCalendar.Data;

namespace LocalCalendar.Calendar
{
    public class DayEventRow : MonoBehaviour
    {
        public enum ViewMode
        {
            Full,
            Compact
        }

        [SerializeField] private TMP_InputField titleLabel;
        [SerializeField] private TMP_InputField timeLabel;
        [SerializeField] private GameObject iconContainer;
        [SerializeField] private GameObject reminderIcon;
        [SerializeField] private GameObject repeatIcon;
        [SerializeField] private TextMeshProUGUI repeatLabel;

        private CalendarItem _item;
        private DateTime _shownOnDate;
        private ViewMode _mode;
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
                timeText = mode == ViewMode.Compact
                    ? $"{occurrenceStart:hh:mm tt}\n – \n{occurrenceEnd:hh:mm tt}"
                    : $"{occurrenceStart:hh:mm tt} – {occurrenceEnd:hh:mm tt}";
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

            reminderIcon.SetActive(_item.Type == CalendarItemType.Reminder);

            repeatIcon.SetActive(_item.RepeatRule != null);
            repeatLabel.text = item.RepeatRule != null
                ? CalendarUtils.RepeatRuleToReadableText(item.RepeatRule)
                : "";
            repeatLabel.gameObject.SetActive(item.RepeatRule != null);

            _mode = mode;
            ApplyMode();
        }

        public void OnClick()
        {
            _onClick?.Invoke((item: _item, shownOnDate: _shownOnDate));
        }

        private void ApplyMode()
        {
            bool isActive = true;
            switch (_mode)
            {
                case ViewMode.Compact:
                    titleLabel.textComponent.fontSize = 30;
                    isActive = false;
                    break;

                default:
                case ViewMode.Full:
                    titleLabel.textComponent.fontSize = 50;
                    isActive = true;
                    break;
            }
            
            iconContainer.SetActive(isActive);
            repeatLabel.gameObject.SetActive(isActive);
        }
    }
}
