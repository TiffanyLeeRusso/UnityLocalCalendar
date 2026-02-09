using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using LocalCalendar.Data;
using LocalCalendar.App;
using LocalCalendar.Services;
using LocalCalendar.Prefabs;

namespace LocalCalendar.Controllers
{
    public class CalendarController : MonoBehaviour, IBackHandler
    {
        [SerializeField] private Header header;
        [SerializeField] private RectTransform monthGrid;
        [SerializeField] private WeekdayHeaderRow weekdayHeader;
        [SerializeField] private DayCell dayCellPrefab;
        [SerializeField] private DayEventsPopup dayEventsPopup;
        [SerializeField] private SidePanelPopover sideMenuPopover;

        private GridLayoutGroup monthGridLayout;
        // Keep track of screen size for orientation changes
        private float _lastWidth;
        private float _lastHeight;

        void Awake()
        {
            monthGridLayout = monthGrid.GetComponent<GridLayoutGroup>();
        }

        void OnEnable()
        {
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


        // --- Orientation-change handling ---

        void Update()
        {
            CheckScreenSize();
        }

        private void CheckScreenSize()
        {
            // Check if dimensions have changed since last frame
            if (Math.Abs(Screen.width - _lastWidth) > 0.1f || Math.Abs(Screen.height - _lastHeight) > 0.1f)
            {
                _lastWidth = Screen.width;
                _lastHeight = Screen.height;
        
                // Trigger the redraw logic
                HandleUpdateLayout();
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
            Vector2 newSize = CalendarUtils.ResizeGrid(monthGridLayout, monthGrid);
            monthGridLayout.cellSize = newSize;

            // If the cells have an AspectRatioFitter or similar, 
            // we need to update the header after the cells resize
            weekdayHeader.Build(newSize.x);

            // Final Pass: Re-enable and rebuild
            monthGridLayout.enabled = true;
            LayoutRebuilder.ForceRebuildLayoutImmediate(monthGrid);
        }

        
        // --- Calendar building ---

        private void RefreshMonth()
        {
            ClearGrid();
            HandleUpdateLayout();

            DateTime _currentMonth = DateContext.CurrentShownMonth;
            DateTime firstDay = new DateTime(_currentMonth.Year,
                                             _currentMonth.Month,
                                             1);

            header.title.text = _currentMonth.ToString("MMMM yyyy");
            header.currentDate = (_currentMonth.Year == DateTime.Today.Year && _currentMonth.Month == DateTime.Today.Month) ?
                DateTime.Today :
                firstDay; // Today if we are on the current month; otherwise, the first of the month

            int startOffset = (int)firstDay.DayOfWeek;
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
            }
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
            RefreshMonth();
        }

        public void NextMonth()
        {
            DateContext.NextMonth();
            RefreshMonth();
        }

        public void Today() {
            DateContext.Today();
            RefreshMonth();
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
