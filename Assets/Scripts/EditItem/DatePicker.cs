using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using LocalCalendar.App;
using LocalCalendar.Calendar;
using LocalCalendar.Services;

namespace LocalCalendar.EditItem
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
        private bool _isInitialized;
        // Keep track of screen size for orientation changes
        // Note since we are instanced we must keep track of these ourselves.
        private float _lastWidth;
        private float _lastHeight;

        public event Action<DateTime> OnDateChanged;

        
        // --- Public interface ---

        public DateTime GetDate() => _selectedDate;

        public void SetDate(DateTime date)
        {
            _selectedDate = date.Date;
            _visibleMonth = new DateTime(date.Year, date.Month, 1);
            RefreshMonth();
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
            _visibleMonth = DateTime.Today;
            _selectedDate = DateTime.Today;
            monthGridLayout = monthGrid.GetComponent<GridLayoutGroup>();
            BuildGrid();
        }

        void Start()
        {
            // Initial setup
            RefreshMonth();
            // Do an immediate layout calculation. This prevents a visible sizing shift.
            SyncLayoutNow();

            _isInitialized = true;

            // Set initial tracking values so Update() doesn't trigger immediately
            _lastWidth = Screen.width;
            _lastHeight = Screen.height;
        }

        void Update()
        {
            CheckScreenSize();
        }

        void OnRectTransformDimensionsChange()
        {
            if (_isInitialized && gameObject.activeInHierarchy)
            {
                HandleUpdateLayout();
            }
        }


        // --- Orientation-change handling ---

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

        private void CheckScreenSize()
        {
            // Check if dimensions have changed since last frame
            if (Math.Abs(Screen.width - _lastWidth) > 0.1f || Math.Abs(Screen.height - _lastHeight) > 0.1f)
            {
                _lastWidth = Screen.width;
                _lastHeight = Screen.height;

                if (_isInitialized)
                {
                    // Trigger the redraw logic
                    HandleUpdateLayout();
                }
            }
        }

        private void HandleUpdateLayout()
        {
            if (gameObject.activeInHierarchy)
            {
                StopAllCoroutines();
                StartCoroutine(UpdateLayout());
            }
        }

        private IEnumerator UpdateLayout()
        {
            // Frame 1: Wait for OS/Unity to acknowledge new resolution
            yield return null; 

            // Frame 2: Disable layout and force Canvas update
            monthGridLayout.enabled = false;
            Canvas.ForceUpdateCanvases();
            yield return null;

            // Frame 3: Apply new math
            ResizeGrid();

            // Final Pass: Re-enable and rebuild
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
            HandleUpdateLayout();

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
