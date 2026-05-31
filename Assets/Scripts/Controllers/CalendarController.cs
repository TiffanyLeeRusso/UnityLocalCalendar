using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using LocalCalendar.Data;
using LocalCalendar.Services;
using LocalCalendar.Utils;
using LocalCalendar.Prefabs;

namespace LocalCalendar.Controllers
{
    public class CalendarController : MonoBehaviour, IBackHandler
    {
        [SerializeField] private RectTransform rootCanvas;
        [SerializeField] private CanvasGroup mainCanvasGroup;
        [SerializeField] private Header header;
        [SerializeField] private RectTransform monthGrid;
        [SerializeField] private WeekdayHeaderRow weekdayHeader;
        [SerializeField] private DayCell dayCellPrefab;
        [SerializeField] private DayEventsPopup dayEventsPopup;
        [SerializeField] private SidePanelPopover sideMenuPopover;

        private GridLayoutGroup monthGridLayout;
        private LayoutElement gridLayoutElement;
        private bool _isRelayouting = false;

        void Awake()
        {
            monthGridLayout = monthGrid.GetComponent<GridLayoutGroup>();
            gridLayoutElement = monthGrid.GetComponent<LayoutElement>();

            // Ensure we have a LayoutElement for the shrinking-for-orientation-change code
            if (gridLayoutElement == null) 
                gridLayoutElement = monthGrid.gameObject.AddComponent<LayoutElement>();
        }

        void OnEnable()
        {
            LayoutWatcher.Instance.OnRelayout += HandleRelayout;

            if (CalendarRefreshSignal.NeedsRefresh)
            {
                CalendarRefreshSignal.NeedsRefresh = false;
                RefreshMonth();
            }
        }

        void OnDisable()
        {
            if (SceneHistoryManager.Exists)
                SceneHistoryManager.Instance.UnregisterHandler(this);

            if (LayoutWatcher.Instance != null)
                LayoutWatcher.Instance.OnRelayout -= HandleRelayout;
        }

        void Start()
        {
            SceneHistoryManager.Instance.RegisterHandler(this);

            header.Configure(new HeaderConfig{ ShowToday = true,
                                               SideMenuPopover = sideMenuPopover });
            header.OnToday += Today;
            header.OnPrev += PrevMonth;
            header.OnNext += NextMonth;

            RefreshMonth();
        }

        // --- Layout handling ---

        private void HandleRelayout()
        {
            if (_isRelayouting) return;
            StopAllCoroutines();
            StartCoroutine(HandleRelayoutRoutine());
        }

        private IEnumerator HandleRelayoutRoutine()
        {
            _isRelayouting = true;

            // Hide everything during calculation or else the grid
            // collapse will show up as a flash of tiny grid.
            // We cannot use gameObject.SetActive(false) because
            // the layout engine stops calculating it entirely
            // and ResizeGrid math will return 0 because the RectTransform
            // doesn't exist while disabled.
            if (mainCanvasGroup != null) mainCanvasGroup.alpha = 0;

            // Collapse the grid so it stops pushing the parent VLG boundaries
            monthGridLayout.enabled = false;
            gridLayoutElement.preferredWidth = 0;
            gridLayoutElement.preferredHeight = 0;

            // Wait for the parent (MainContent) to shrink to the new screen size
            yield return null; // Wait 1 frame
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(rootCanvas);

            UpdateGridSizing();

            // Show the UI
            if (mainCanvasGroup != null) mainCanvasGroup.alpha = 1;
            _isRelayouting = false;
        }

        private void UpdateGridSizing()
        {
            // Simple sizing logic that doesn't use Coroutines/Alpha
            Vector2 newSize = CalendarUtils.ResizeGrid(monthGridLayout, monthGrid);
            monthGridLayout.cellSize = newSize;
            weekdayHeader.Build(newSize.x);

            monthGridLayout.enabled = true;
            LayoutRebuilder.ForceRebuildLayoutImmediate(monthGrid);
        
            // Reset preferred size so it doesn't stay at 0
            gridLayoutElement.preferredWidth = -1;
            gridLayoutElement.preferredHeight = -1;

            LayoutRebuilder.ForceRebuildLayoutImmediate(monthGrid);
        }

        // --- Calendar building ---

        private void RefreshMonth(bool fullLayoutCalc = true, bool animateToday = false)
        {
            ClearGrid();
            DayCell today = BuildGridCells();

            if(fullLayoutCalc)
            {
                //HandleRelayout();
                StartCoroutine(ExecuteRelayoutWithAnimation(today, animateToday));
            }
            else
            {
                UpdateGridSizing();
                if (animateToday && today != null) today.PlayTodayHighlight();
            }
        }

        // Helper to wait for the relayout fade before animating today
        private IEnumerator ExecuteRelayoutWithAnimation(DayCell cell, bool shouldAnimate)
        {
            yield return StartCoroutine(HandleRelayoutRoutine());
    
            if (shouldAnimate && cell != null)
            {
                cell.PlayTodayHighlight();
            }
        }

        private DayCell BuildGridCells()
        {
            DayCell todayCell = null; // Track the "today" cell for animation on Today-button press
            DateTime _currentMonth = DateContext.CurrentShownMonth;
            DateTime firstDay = new DateTime(_currentMonth.Year,
                                             _currentMonth.Month,
                                             1);

            header.title.text = _currentMonth.ToString("MMM yyyy");
            header.currentDate = (_currentMonth.Year == DateTime.Today.Year && _currentMonth.Month == DateTime.Today.Month) ?
                DateTime.Today :
                firstDay; // Today if we are on the current month; otherwise, the first of the month

            int startOffset = CalendarUtils.GetStartOffset(firstDay);
            DateTime gridStart = firstDay.AddDays(-startOffset);

            for (int i = 0; i < 42; i++)
            {
                DateTime date = gridStart.AddDays(i);
                bool isCurrentMonth = date.Month == _currentMonth.Month;

                bool isToday = date.Date == DateTime.Today;
                var dayItems = CalendarUtils.GetExpandedDayItems(new CalendarRepository(), date);
                var cell = Instantiate(dayCellPrefab, monthGrid);
                cell.Initialize(
                    date,
                    isToday,
                    isCurrentMonth,
                    dayItems,
                    OnDayClicked,
                    OnItemClicked);

                if (date.Date == DateTime.Today) todayCell = cell;
            }

            return todayCell;
        }

        private void ClearGrid()
        {
            foreach (Transform child in monthGrid)
                Destroy(child.gameObject);
        }


        // --- Click handlers ---

        public bool OnBackButtonPressed()
        {
            if (dayEventsPopup.gameObject.activeSelf)
            {
                dayEventsPopup.gameObject.SetActive(false);
                return true;
            }
            return false; // Let the manager switch scenes
        }

        public void PrevMonth()
        {
            DateContext.PrevMonth();
            RefreshMonth(false);
        }

        public void NextMonth()
        {
            DateContext.NextMonth();
            RefreshMonth(false);
        }

        public void Today() {
            DateContext.Today();
            RefreshMonth(false, true);
        }

        public void OnDayClicked(DateTime date)
        {
            dayEventsPopup.Show(date);
        }

        private void OnItemClicked((CalendarItem item, DateTime shownOnDate) args)
        {
            OnDayClicked(args.shownOnDate);
        }
    }
}
