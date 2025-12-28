using System;
using TMPro;
using UnityEngine;

namespace LocalCalendar.Schedule
{
    public class ScheduleDayHeader : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI label;

        public void SetDate(DateTime date)
        {
            label.text = date.ToString("dddd, MMM d");
        }
    }
}
