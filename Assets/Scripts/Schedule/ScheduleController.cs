using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using LocalCalendar.Data;
using LocalCalendar.Models;
using UnityEngine.SceneManagement;
using LocalCalendar.App;
using LocalCalendar.Calendar;

namespace LocalCalendar.Schedule
{
    public class ScheduleController : MonoBehaviour
    {
        [SerializeField] private Transform content;
        [SerializeField] private ScheduleDayHeader dayHeaderPrefab;
        [SerializeField] private DayEventRow itemRowPrefab;

        private CalendarRepository _repo;

        void Start()
        {
            _repo = new CalendarRepository();
            BuildSchedule();
        }

        private void BuildSchedule()
        {
            Clear();

            DateTime start = DateTime.Today;
            DateTime end = start.AddDays(30);

            var items = _repo.GetAll();

            var expanded = new List<(DateTime date, CalendarItem item)>();

            foreach (var item in items)
            {
                // Simple version: no recurrence yet
                var localDate = item.StartUtc.ToLocalTime();
                if (localDate.Date >= start && localDate.Date <= end)
                {
                    expanded.Add((localDate.Date, item));
                }
            }

            var grouped = expanded
                .GroupBy(e => e.date)
                .OrderBy(g => g.Key);

            foreach (var group in grouped)
            {
                var header = Instantiate(dayHeaderPrefab, content);
                header.SetDate(group.Key);

                foreach (var entry in group.OrderBy(e => e.item.StartUtc))
                {
                    var row = Instantiate(itemRowPrefab, content);
                    row.Initialize(entry.item, OnItemClicked);
                }
            }
        }

        private void OnItemClicked(CalendarItem item)
        {
            EditItemContext.EditingItemId = item.Id;
            SceneManager.LoadScene("EditItemScene");
        }

        private void Clear()
        {
            foreach (Transform child in content)
                Destroy(child.gameObject);
        }

        public void Add()
        {
            // To open create/edit scene
            EditItemContext.SelectedDate = DateTime.Today;
            SceneManager.LoadScene("EditItemScene");
        }

        public void Back()
        {
            SceneManager.LoadScene("CalendarScene");
        }
    }
}
