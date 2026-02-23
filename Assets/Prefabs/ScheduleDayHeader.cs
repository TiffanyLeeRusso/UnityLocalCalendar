using System;
using TMPro;
using UnityEngine;

namespace LocalCalendar.Prefabs
{
    public class ScheduleDayHeader : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI label;
        [SerializeField] private GameObject highlight;

        public void SetDate(DateTime date)
        {
            label.text = date.ToString("dddd, MMM d");
        }

        public void SetHighlight(bool isActive = false)
        {
            highlight.SetActive(isActive);
        }
    }
}
