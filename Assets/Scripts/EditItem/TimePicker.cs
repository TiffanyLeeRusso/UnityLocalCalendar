using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LocalCalendar.EditItem
{
    public class TimePicker : MonoBehaviour
    {
        [SerializeField] private TMP_Dropdown hourDropdown;
        [SerializeField] private TMP_Dropdown minuteDropdown;
        [SerializeField] private Toggle amPmToggle; // off = AM, on = PM

        public event Action<DateTime> OnTimeChanged;

        private bool _suppressEvents;

        void Awake()
        {
            PopulateHours();
            PopulateMinutes();

            hourDropdown.onValueChanged.AddListener(_ => NotifyChanged());
            minuteDropdown.onValueChanged.AddListener(_ => NotifyChanged());
            amPmToggle.onValueChanged.AddListener(_ => NotifyChanged());
        }

        private void PopulateHours()
        {
            hourDropdown.ClearOptions();

            var options = new List<string>();
            for (int h = 1; h <= 12; h++)
                options.Add(h.ToString());

            hourDropdown.AddOptions(options);
        }

        private void PopulateMinutes()
        {
            minuteDropdown.ClearOptions();

            var options = new List<string>();
            for (int m = 0; m < 60; m++)
                options.Add(m.ToString("00"));

            minuteDropdown.AddOptions(options);
        }

        public void SetTime(DateTime time)
        {
            _suppressEvents = true;

            int hour24 = time.Hour;
            int minute = time.Minute;

            bool isPm = hour24 >= 12;
            int hour12 = hour24 % 12;
            if (hour12 == 0) hour12 = 12;

            hourDropdown.value = hour12 - 1;
            minuteDropdown.value = Mathf.Clamp(minute, 0, 59);
            amPmToggle.isOn = isPm;

            _suppressEvents = false;
        }

        public TimeSpan GetTime()
        {
            int hour12 = hourDropdown.value + 1;
            int minute = minuteDropdown.value;
            bool isPm = amPmToggle.isOn;

            int hour24 = hour12 % 12;
            if (isPm) hour24 += 12;

            return new TimeSpan(hour24, minute, 0);
        }

        private void NotifyChanged()
        {
            if (_suppressEvents)
                return;

            // Date portion is ignored by controller
            OnTimeChanged?.Invoke(DateTime.Today + GetTime());
        }
    }
}
