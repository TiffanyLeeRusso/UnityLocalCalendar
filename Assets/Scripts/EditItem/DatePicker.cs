using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using LocalCalendar.Calendar;
using LocalCalendar.Services;

namespace LocalCalendar.EditItem
{
    public class DatePicker : MonoBehaviour
    {
        [SerializeField] private TMP_Text monthLabel;
        [SerializeField] private RectTransform monthGrid;
        [SerializeField] private DayCellButton dayCellPrefab;

        private readonly List<DayCellButton> _cells = new();

        private DateTime _visibleMonth;
        private DateTime _selectedDate;
        private GridLayoutGroup monthGridLayout;
        private bool _didInitialResize;

        public event Action<DateTime> OnDateChanged;

        void Awake()
        {
            _visibleMonth = DateTime.Today;
            _selectedDate = DateTime.Today;
            monthGridLayout = monthGrid.GetComponent<GridLayoutGroup>();
            BuildGrid();
        }

        void OnEnable()
        {
            Canvas.willRenderCanvases += OnWillRenderCanvases;
            if (CalendarRefreshSignal.NeedsRefresh)
            {
                CalendarRefreshSignal.NeedsRefresh = false;
                Refresh();
            }
        }

        void OnDisable()
        {
            Canvas.willRenderCanvases -= OnWillRenderCanvases;
        }

        private void OnWillRenderCanvases()
        {
            if (_didInitialResize) return;

            Refresh();
            _didInitialResize = true;
        }

        public void SetDate(DateTime date)
        {
            _selectedDate = date.Date;
            _visibleMonth = new DateTime(date.Year, date.Month, 1);
            Refresh();
        }

        public DateTime GetDate() => _selectedDate;

        public void NextMonth()
        {
            _visibleMonth = _visibleMonth.AddMonths(1);
            Refresh();
        }

        public void PrevMonth()
        {
            _visibleMonth = _visibleMonth.AddMonths(-1);
            Refresh();
        }

        // -------- INTERNAL --------

        private void BuildGrid()
        {
            for (int i = 0; i < 42; i++)
            {
                var cell = Instantiate(dayCellPrefab, monthGrid);
                _cells.Add(cell);
            }
        }

        private void Refresh()
        {
            monthLabel.text = _visibleMonth.ToString("MMMM yyyy");

            int year = _visibleMonth.Year;
            int month = _visibleMonth.Month;

            int firstWeekday = CalendarUtils.FirstWeekdayOfMonth(year, month);
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

            monthGridLayout.cellSize = CalendarUtils.ResizeGrid(monthGridLayout, monthGrid);
        }

        private void OnCellClicked(DateTime date)
        {
            _selectedDate = date.Date;
            OnDateChanged?.Invoke(_selectedDate);
            Refresh();
        }
    }
}
