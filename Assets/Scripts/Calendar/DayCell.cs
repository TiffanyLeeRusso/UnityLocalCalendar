using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LocalCalendar.Calendar
{
    public class DayCell : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI dayNumber;
        [SerializeField] private Image background;
        [SerializeField] private GameObject marker;

        private DateTime _date;
        private Action<DateTime> _onClick;

        public void Initialize(
            DateTime date,
            bool isToday,
            bool hasItems,
            Action<DateTime> onClick)
        {
            _date = date;
            _onClick = onClick;

            dayNumber.gameObject.SetActive(true);
            dayNumber.text = date.Day.ToString();
            marker.SetActive(hasItems);

            background.color = isToday
                ? new Color(0.5f, 0.3f, 0.9f)
                : Color.clear;
        }

        public void OnClick()
        {
            _onClick?.Invoke(_date);
        }
    }
}

