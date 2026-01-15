using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using LocalCalendar.Data;
using LocalCalendar.Models;
using LocalCalendar.App;
using LocalCalendar.Calendar;
using LocalCalendar.EditItem;
using LocalCalendar.Services;

namespace LocalCalendar.Schedule
{
    public class ScheduleController : MonoBehaviour
    {
        [SerializeField] private TMP_Text monthLabel;
        [SerializeField] private Transform content;
        [SerializeField] private ScheduleDayHeader dayHeaderPrefab;
        [SerializeField] private DayEventRow itemRowPrefab;

        private CalendarRepository _repo;
        private DateTime _currentMonth;

        void Start()
        {
            _repo = new CalendarRepository();
            _currentMonth = ScheduleContext.InitialMonth ??
                new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

            ScheduleContext.Clear();
            BuildSchedule();
        }

        public void PrevMonth()
        {
            _currentMonth = _currentMonth.AddMonths(-1);
            BuildSchedule();
        }

        public void NextMonth()
        {
            _currentMonth = _currentMonth.AddMonths(1);
            BuildSchedule();
        }

        private void BuildSchedule()
        {
            Clear();

            monthLabel.text = _currentMonth.ToString("MMMM yyyy");

            DateTime rangeStart = _currentMonth;
            DateTime rangeEnd = _currentMonth.AddMonths(1).AddDays(-1);

            var grouped = CalendarUtils.GetExpandedMonthItems(_repo, rangeStart, rangeEnd);
            foreach (var groupItem in grouped)
            {
                var header = Instantiate(dayHeaderPrefab, content);
                header.SetDate(groupItem.Key);

                foreach (var entry in groupItem
                         .OrderBy(e => e.item.StartUtc.ToLocalTime()))
                {
                    var row = Instantiate(itemRowPrefab, content);
                    row.Initialize(entry.item, groupItem.Key, OnItemClicked);
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
