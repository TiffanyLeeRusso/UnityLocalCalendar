using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using LocalCalendar.Data;
using LocalCalendar.App;

namespace LocalCalendar.Calendar
{
    public class CalendarController : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private TextMeshProUGUI monthLabel;
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

            var all = _repo.GetAll();
            foreach (var item in all)
            {
                result.Add(item.StartUtc.ToLocalTime().Date);
            }

            return result;
        }

        private void ClearGrid()
        {
            foreach (Transform child in monthGrid)
                Destroy(child.gameObject);
        }

        public void OnDayClicked(DateTime date)
        {
            Debug.Log("Clicked day: " + date.ToShortDateString());
            var items = _repo.GetItemsForDay(date);
            dayEventsPopup.Show(date, items);
        }

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
