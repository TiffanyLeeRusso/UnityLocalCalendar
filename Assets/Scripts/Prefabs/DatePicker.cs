using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using LocalCalendar.App;
using LocalCalendar.Services;
using LocalCalendar.Utils;

namespace LocalCalendar.Prefabs
{
    public class DatePicker : MonoBehaviour
    {
        [SerializeField] private TMP_Text monthLabel;
        [SerializeField] private WeekdayHeaderRow weekdayHeader;
        [SerializeField] private RectTransform monthGrid;
        [SerializeField] private DayCellButton dayCellPrefab;

        private readonly List<DayCellButton> _cells = new();

        private DateTime _visibleMonth;
        private DateTime _selectedDate;
        private GridLayoutGroup monthGridLayout;
        private bool _initialized;

        public event Action<DateTime> OnDateChanged;

        void OnEnable()
        {
            if (LayoutWatcher.Instance != null)
                LayoutWatcher.Instance.OnRelayout += HandleRelayout;

            EnsureInitialized();
            RefreshMonth();
            SyncLayoutNow();
        }

        void OnDisable()
        {
            if (LayoutWatcher.Instance != null)
                LayoutWatcher.Instance.OnRelayout -= HandleRelayout;
        }

        
        // --- Public interface ---

        public DateTime GetDate() => _selectedDate;

        public void SetDate(DateTime date)
        {
            EnsureInitialized();

            _selectedDate = date.Date;
            _visibleMonth = new DateTime(date.Year, date.Month, 1);

            RefreshMonth();
            SyncLayoutNow();
        }

        public void NextMonth()
        {
            _visibleMonth = _visibleMonth.AddMonths(1);
            RefreshMonth();
        }

        public void PrevMonth()
        {
            _visibleMonth = _visibleMonth.AddMonths(-1);
            RefreshMonth();
        }


        // --- Initialize ---
        
        void Awake()
        {
            //_visibleMonth = DateTime.Today;
            //_selectedDate = DateTime.Today;
            monthGridLayout = monthGrid.GetComponent<GridLayoutGroup>();
            //BuildGrid();
        }

        /*
        void Start()
        {
            // Initial setup
            RefreshMonth();
            // Do an immediate layout calculation. This prevents a visible sizing shift.
            SyncLayoutNow();
            }*/

        private void EnsureInitialized()
        {
            if (_initialized) return;

            monthGridLayout = monthGrid.GetComponent<GridLayoutGroup>();

            if (_cells.Count == 0)
                BuildGrid();

            if (_selectedDate == default)
                _selectedDate = DateTime.Today;

            if (_visibleMonth == default)
                _visibleMonth = new DateTime(_selectedDate.Year, _selectedDate.Month, 1);

            _initialized = true;
        }

        
        // --- Layout handling ---

        private void ResizeGrid()
        {
            Vector2 newSize = CalendarUtils.ResizeGrid(monthGridLayout, monthGrid);
            monthGridLayout.cellSize = newSize;
            weekdayHeader.Build(newSize.x);
        }
            
        private void SyncLayoutNow()
        {
            if (monthGridLayout == null) return;

            // We don't use the Coroutine here because we want it done THIS frame to avoid visible shifting.
            Canvas.ForceUpdateCanvases();
            ResizeGrid();
            LayoutRebuilder.ForceRebuildLayoutImmediate(monthGrid);
        }

        private void HandleRelayout()
        {
            if (gameObject.activeInHierarchy)
            {
                StopAllCoroutines();
                StartCoroutine(RelayoutRoutine());
            }
        }

        private IEnumerator RelayoutRoutine()
        {
            monthGridLayout.enabled = false;

            yield return null; 
            Canvas.ForceUpdateCanvases();
            yield return null;

            ResizeGrid();

            monthGridLayout.enabled = true;
            LayoutRebuilder.ForceRebuildLayoutImmediate(monthGrid);
        }
        
        // --- Calendar builder ---

        private void BuildGrid()
        {
            for (int i = 0; i < 42; i++)
            {
                var cell = Instantiate(dayCellPrefab, monthGrid);
                _cells.Add(cell);
            }
        }

        private void RefreshMonth()
        {
            monthLabel.text = _visibleMonth.ToString("MMMM yyyy");

            int year = _visibleMonth.Year;
            int month = _visibleMonth.Month;

            DateTime firstDay = new DateTime(year, month, 1);
            int firstWeekday = CalendarUtils.GetStartOffset(firstDay);
            int daysInMonth = CalendarUtils.DaysInMonth(year, month);

            DateTime gridStart =
                new DateTime(year, month, 1).AddDays(-firstWeekday);

            for (int i = 0; i < _cells.Count; i++)
            {
                DateTime cellDate = gridStart.AddDays(i);
                bool isCurrentMonth = cellDate.Month == month;
                bool isSelected = cellDate.Date == _selectedDate;

                _cells[i].Initialize(
                    cellDate,
                    isCurrentMonth,
                    isSelected,
                    OnCellClicked);
            }
        }


        // --- Click handlers ---

        private void OnCellClicked(DateTime date)
        {
            _selectedDate = date.Date;
            OnDateChanged?.Invoke(_selectedDate);
            RefreshMonth(); // highlight new selection
        }
    }
}
