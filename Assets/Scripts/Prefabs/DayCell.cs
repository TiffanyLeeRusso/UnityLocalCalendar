using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using LocalCalendar.Data;
using LocalCalendar.Utils;

namespace LocalCalendar.Prefabs
{
    public class DayCell : MonoBehaviour
    {
        [SerializeField] private Button cellButton;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private TextMeshProUGUI dayNumber;
        [SerializeField] private RectTransform dayNumberRT;
        [SerializeField] private GameObject highlight;
        [SerializeField] private RectTransform eventsContainer;
        [SerializeField] private DayEventRow dayEventRowPrefab;

        private DateTime _date;
        private Action<DateTime> _onDayClicked;
        private Action<(CalendarItem item, DateTime shownOnDate)> _onItemClicked;
        // For the "today" effect
        private Vector3 originalScale = Vector3.one;
        private Color originalColor;
        private Color todayEffectColor;

        void Awake()
        {
            originalColor = backgroundImage.color;
            todayEffectColor = new Color(0.17f, 0.17f, .65f, 1f);
        }

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

        public void Layout(float fontSize = 50, bool showEvents = true, bool allowCellClick = true)
        {
            dayNumber.fontSize = fontSize;
            var dayNumHeight = fontSize + 10f;
            dayNumberRT.sizeDelta = new Vector2(dayNumberRT.sizeDelta.x, dayNumHeight);
            dayNumberRT.anchoredPosition = new Vector2(dayNumberRT.anchoredPosition.x, -(dayNumHeight/2));

            eventsContainer.gameObject.SetActive(showEvents);

            // Enable or disable cell interactivity so clicks pass through
            var cellButton = gameObject.GetComponent<Button>();
            if (cellButton != null) cellButton.enabled = allowCellClick;
            var cellTrigger = gameObject.GetComponent<EventTrigger>();
            if (cellTrigger != null) cellTrigger.enabled = allowCellClick;
        }

        private void BuildRows(List<(CalendarItem item, DateTime occurrenceStart)> items)
        {
            foreach (Transform child in eventsContainer)
                Destroy(child.gameObject);

            foreach (var item in items)
            {
                var row = Instantiate(dayEventRowPrefab, eventsContainer);
                row.Initialize(item.item, item.occurrenceStart, _onItemClicked, _date, DayEventRow.ViewMode.Compact);
                var visuals = row.GetComponent<CalendarItemVisuals>();
                visuals.ApplyStyle(item.item.Color);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(eventsContainer);
        }

        // Called by Button / EventTrigger on the cell background
        public void OnDayClicked()
        {
            _onDayClicked?.Invoke(_date);
        }

        public void PlayTodayHighlight()
        {
            StopAllCoroutines();
            StartCoroutine(TodayPopRoutine());
        }

        // Scale the DayCell and change the background color
        private IEnumerator TodayPopRoutine()
        {
            float duration = 0.5f;
            float elapsed = 0;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
            
                // Animation Curve: Up and then back down (Sin wave)
                float curve = Mathf.Sin(t * Mathf.PI); 
            
                // Scale up to 1.5x
                gameObject.transform.localScale = originalScale * (1f + (curve * 0.5f));
                // Flash the background color
                backgroundImage.color = Color.Lerp(originalColor, todayEffectColor, curve);
                yield return null;
            }

            // Reset to original state
            gameObject.transform.localScale = originalScale;
            backgroundImage.color = originalColor;
        }
    }
}
