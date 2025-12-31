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

        private readonly int[] minuteSteps = { 0, 5, 10, 15, 20, 25, 30, 35, 40, 45, 50, 55 };

        void Awake()
        {
            PopulateHours();
            PopulateMinutes();
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
            foreach (var m in minuteSteps)
                options.Add(m.ToString("00"));

            minuteDropdown.AddOptions(options);
        }

        /// <summary>
        /// Set picker UI from a DateTime (local time).
        /// </summary>
        public void SetTime(DateTime time)
        {
            int hour24 = time.Hour;
            int minute = time.Minute;

            bool isPm = hour24 >= 12;
            int hour12 = hour24 % 12;
            if (hour12 == 0) hour12 = 12;

            hourDropdown.value = hour12 - 1;
            amPmToggle.isOn = isPm;

            int minuteIndex = Array.IndexOf(minuteSteps, (minute / 5) * 5);
            if (minuteIndex < 0) minuteIndex = 0;

            minuteDropdown.value = minuteIndex;
        }

        /// <summary>
        /// Get selected time as a TimeSpan.
        /// </summary>
        public TimeSpan GetTime()
        {
            int hour12 = hourDropdown.value + 1;
            int minute = minuteSteps[minuteDropdown.value];
            bool isPm = amPmToggle.isOn;

            int hour24 = hour12 % 12;
            if (isPm) hour24 += 12;

            return new TimeSpan(hour24, minute, 0);
        }
    }
}
