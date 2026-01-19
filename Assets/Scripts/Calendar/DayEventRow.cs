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

        [SerializeField] private ViewMode mode;
        [SerializeField] private TMP_InputField timeLabel;
        [SerializeField] private TMP_InputField titleLabel;
        [SerializeField] private GameObject reminderIcon;
        [SerializeField] private GameObject repeatIcon;
        [SerializeField] private TextMeshProUGUI repeatLabel;

        private CalendarItem _item;
        private Action<CalendarItem> _onClick;

        void Awake()
        {
            ApplyMode();
        }

        public void SetMode(ViewMode newMode)
        {
            mode = newMode;
            ApplyMode();
        }

        public void Initialize(CalendarItem item, DateTime occurrenceStart, Action<CalendarItem> onClick)
        {
            _item = item;
            _onClick = onClick;

            DateTime occurrenceEnd = CalendarUtils.GetOccurrenceEnd(item, occurrenceStart);
            bool isSingleDay = occurrenceStart.Date == occurrenceEnd.Date;

            string timeText;
            if (item.AllDay && item.StartUtc.TimeOfDay == TimeSpan.Zero)
                timeText = "All day";
            else if (isSingleDay) // TODO: newlines cause small text because height is set for one line
                timeText = mode == ViewMode.Compact
                    ? $"{occurrenceStart:hh:mm tt}\n – \n{occurrenceEnd:hh:mm tt}"
                    : $"{occurrenceStart:hh:mm tt} – {occurrenceEnd:hh:mm tt}";
            else
                timeText = $"{occurrenceStart:hh:mm tt} → {occurrenceEnd:hh:mm tt}";

            timeLabel.text = timeText;

            titleLabel.text = item.Title;

            reminderIcon.SetActive(_item.Type == CalendarItemType.Reminder);

            repeatIcon.SetActive(_item.RepeatRule != null);
            repeatLabel.text = item.RepeatRule != null
                ? CalendarUtils.RepeatRuleToReadableText(item.RepeatRule)
                : "";
            repeatLabel.gameObject.SetActive(item.RepeatRule != null);
        }

        public void OnClick()
        {
            // open EditItemScene
            _onClick?.Invoke(_item);
        }

        private void ApplyMode()
        {
            var rect = gameObject.GetComponent<RectTransform>();
            switch (mode)
            {
                case ViewMode.Compact:
                    //rect.localScale = new Vector3(0.5f, 0.5f, 1f);
                    repeatLabel.gameObject.SetActive(false);
                    break;

                default:
                case ViewMode.Full:
                    //rect.localScale = new Vector3(1f, 1f, 1f);
                    repeatLabel.gameObject.SetActive(true);
                    break;
            }
            
            //LayoutRebuilder.ForceRebuildLayoutImmediate(rect); // Need the parent RectTransform..
            //Canvas.ForceUpdateCanvases();
        }
    }
}
