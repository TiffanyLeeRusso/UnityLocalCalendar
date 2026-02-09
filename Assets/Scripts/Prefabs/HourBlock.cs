using UnityEngine;
using TMPro;
using System;

namespace LocalCalendar.Prefabs
{
    public class HourBlock : MonoBehaviour
    {
        [SerializeField] private TMP_Text hourLabel;
        [SerializeField] private Transform eventsRoot;

        public Transform EventsRoot => eventsRoot;

        public void SetHour(int hour)
        {
            hourLabel.text = DateTime.Today.AddHours(hour).ToString("h\ntt");
        }
    }
}

