using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using LocalCalendar.App;
using LocalCalendar.Data;
using UnityEngine.SceneManagement;

namespace LocalCalendar.Calendar
{
    public class DayEventsPopup : MonoBehaviour
    {
        [SerializeField] private TMP_InputField dateLabel;
        [SerializeField] private Transform content;
        [SerializeField] private DayEventRow rowPrefab;

        private DateTime _date;

        public void Show(DateTime date, List<CalendarItem> items)
        {
            _date = date;

            gameObject.SetActive(true);
            dateLabel.text = date.ToString("dddd, MMM d");

            Clear();

            // ORDER BY START TIME (local)
            items.Sort((a, b) =>
                       a.StartUtc.ToLocalTime().CompareTo(b.StartUtc.ToLocalTime()));

            foreach (var item in items)
            {
                var row = Instantiate(rowPrefab, content);
                row.Initialize(item, date, OnItemClicked);
            }
        }

        public void Add()
        {
            // To open create/edit scene
            EditItemContext.SelectedDate = _date;//DateTime.Today;
            SceneManager.LoadScene("EditItemScene");
        }

        private void OnItemClicked(CalendarItem item)
        {
            EditItemContext.EditingItemId = item.Id;
            EditItemContext.SelectedDate = null;

            SceneManager.LoadScene("EditItemScene");
        }
        
        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void Clear()
        {
            foreach (Transform child in content)
                Destroy(child.gameObject);
        }
    }
}
