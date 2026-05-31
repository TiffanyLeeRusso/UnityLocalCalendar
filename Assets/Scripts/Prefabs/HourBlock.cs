using UnityEngine;
using TMPro;
using System;
using LocalCalendar.Services;
using LocalCalendar.Utils;

namespace LocalCalendar.Prefabs
{
    public class HourBlock : MonoBehaviour
    {
        [SerializeField] private TMP_Text hourLabel;

        public void SetHour(int hour)
        {
            hourLabel.text = AppUtils.FormatTime(DateTime.Today.AddHours(hour),
                                                 compactMode: true);
        }
    }
}
