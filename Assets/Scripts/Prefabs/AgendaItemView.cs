using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using LocalCalendar.Data;

namespace LocalCalendar.Prefabs
{
    public class AgendaItemView : MonoBehaviour
    {
        [SerializeField] TMP_Text title;
        [SerializeField] Button buttonOverlay;

        private CalendarItem _item;

        public void Bind(CalendarItem item, Action<CalendarItem> onClick)
        {
            _item = item;
            title.text = item.Title;

            buttonOverlay.onClick.RemoveAllListeners();
            buttonOverlay.onClick.AddListener(() => onClick?.Invoke(item));
        }
    }
}
