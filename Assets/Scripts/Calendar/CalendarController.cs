using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using LocalCalendar.Data;
using LocalCalendar.App;
using LocalCalendar.Services;

namespace LocalCalendar.Calendar
{
    public class CalendarController : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private TMP_InputField monthLabel;
        [SerializeField] private Transform monthGrid;
        [SerializeField] private DayCell dayCellPrefab;
        [SerializeField] private DayEventsPopup dayEventsPopup;

        private DateTime _currentMonth;
        private CalendarRepository _repo;

        void Awake()
        {
            _repo = new CalendarRepository();
            _currentMonth = DateTime.Today;
        }

        void OnEnable()
        {
            if (CalendarRefreshSignal.NeedsRefresh)
            {
                CalendarRefreshSignal.NeedsRefresh = false;
                RefreshMonth();
            }
        }

        void Start()
        {
            RefreshMonth();
        }

        public void PrevMonth()
        {
            _currentMonth = _currentMonth.AddMonths(-1);
            RefreshMonth();
        }

        public void NextMonth()
        {
            _currentMonth = _currentMonth.AddMonths(1);
            RefreshMonth();
        }

        public void Today() {
            _currentMonth = DateTime.Today;
            RefreshMonth();
        }
        
        private void RefreshMonth()
        {
            ClearGrid();

            monthLabel.text = _currentMonth.ToString("MMMM yyyy");

            DateTime firstDay = new DateTime(
                _currentMonth.Year,
                _currentMonth.Month,
                1);

            int startOffset = (int)firstDay.DayOfWeek;
            DateTime gridStart = firstDay.AddDays(-startOffset);

            var itemsByDay = LoadItemsForMonth();

            for (int i = 0; i < 42; i++)
            {
                DateTime date = gridStart.AddDays(i);

                bool isToday = date.Date == DateTime.Today;
                bool hasItems = itemsByDay.Contains(date.Date);

                var cell = Instantiate(dayCellPrefab, monthGrid);
                Debug.Log(date.Day.ToString());
                Debug.Log(hasItems);
                cell.Initialize(
                    date,
                    isToday,
                    hasItems,
                    OnDayClicked);
            }
        }

        private HashSet<DateTime> LoadItemsForMonth()
        {
            var result = new HashSet<DateTime>();

            DateTime monthStart = new DateTime(
                _currentMonth.Year,
                _currentMonth.Month,
                1);

            DateTime monthEnd = monthStart.AddMonths(1).AddDays(-1);

            var all = _repo.GetAllCalendarItems();
            foreach (var item in all)
            {
                foreach (var occurrence in RecurrenceExpander.ExpandOccurrences(
                             item, monthStart, monthEnd))
                {
                    DateTime occStart = occurrence;
                    DateTime occEnd = GetOccurrenceEnd(item, occStart);

                    for (DateTime d = occStart.Date; d < occEnd.Date; d = d.AddDays(1))
                        result.Add(d);
                }
            }
            /*
            foreach (var item in all)
            {
                foreach (var occurrence in
                 RecurrenceExpander.ExpandOccurrences(
                     item,
                     monthStart,
                     monthEnd))
                {
                    result.Add(occurrence.Date);
                }
                //result.Add(item.StartUtc.ToLocalTime().Date);
            }
            */
            MyDebug.DumpObj(result);
            return result;
        }

        private DateTime GetOccurrenceEnd(CalendarItem item, DateTime occurrenceStart)
        {
            if (item.EndUtc == item.StartUtc)
                return occurrenceStart.AddMinutes(1);

            TimeSpan duration = item.EndUtc - item.StartUtc;

            return occurrenceStart.Add(duration);
        }


        private void ClearGrid()
        {
            foreach (Transform child in monthGrid)
                Destroy(child.gameObject);
        }

        public void OnDayClicked(DateTime date)
        {
            var items = _repo.GetAllCalendarItems();

            var dayItems = new List<CalendarItem>();

            foreach (var item in items)
            {
                if (item.RepeatRule == null)
                {
                    if (item.StartUtc.ToLocalTime().Date == date.Date)
                        dayItems.Add(item);
                }
                else
                {
                    foreach (var occ in RecurrenceExpander
                             .ExpandOccurrences(item, date, date))
                    {
                        dayItems.Add(item);
                    }
                }
            }

            dayEventsPopup.Show(date, dayItems);
        }

        /*
        public void OnDayClicked(DateTime date)
        {
            Debug.Log("Clicked day: " + date.ToShortDateString());
            var items = _repo.GetItemsForDay(date);
            dayEventsPopup.Show(date, items);
            }*/

        public void OpenSchedule()
        {
            SceneManager.LoadScene("ScheduleScene");
        }

        public void OpenSettings()
        {
            SceneManager.LoadScene("SettingsScene");
        }
    }
}
