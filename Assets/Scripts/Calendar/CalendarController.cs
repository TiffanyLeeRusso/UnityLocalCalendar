using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using LocalCalendar.Data;
using LocalCalendar.App;
using LocalCalendar.Services;
using LocalCalendar.EditItem;
using LocalCalendar.Schedule;

namespace LocalCalendar.Calendar
{
    public class CalendarController : MonoBehaviour, IBackHandler
    {
        [SerializeField] private TMP_Text monthLabel;
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
            monthLabel.text = _currentMonth.ToString("MMMM yyyy");

            DateTime firstDay = new DateTime(_currentMonth.Year,
                                             _currentMonth.Month,
                                             1);

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

            //Canvas.ForceUpdateCanvases();
            //LayoutRebuilder.ForceRebuildLayoutImmediate(monthGrid);
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
            else if (sideMenuPopover.gameObject.activeSelf)
            {
                sideMenuPopover.gameObject.SetActive(false);
                return true;
            }
            return false; // Let the manager switch scenes
        }
        
        public void OpenSideMenu()
        {
            sideMenuPopover.gameObject.SetActive(true);
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

        public void Add()
        {
            // To open create/edit scene
            EditItemContext.SelectedDate = DateTime.Today;
            SceneHistoryManager.Instance.LoadScene(AppScene.EditItem);
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
