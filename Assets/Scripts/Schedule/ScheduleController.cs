using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using LocalCalendar.Data;
using LocalCalendar.Calendar;
using LocalCalendar.Services;
using LocalCalendar.EditItem;

namespace LocalCalendar.Schedule
{
    public class ScheduleController : MonoBehaviour, IBackHandler
    {
        [SerializeField] private TMP_Text monthLabel;
        [SerializeField] private Transform content;
        [SerializeField] private ScheduleDayHeader dayHeaderPrefab;
        [SerializeField] private DayEventRow itemRowPrefab;
        [SerializeField] private RectTransform paddingPrefab;
        [SerializeField] private SidePanelPopover sideMenuPopover;

        void OnEnable()
        {

            if (CalendarRefreshSignal.NeedsRefresh)
            {
                CalendarRefreshSignal.NeedsRefresh = false;
                BuildSchedule();
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
            BuildSchedule();
        }

        public void PrevMonth()
        {
            DateContext.PrevMonth();
            BuildSchedule();
        }

        public void NextMonth()
        {
            DateContext.NextMonth();
            BuildSchedule();
        }

        private void BuildSchedule()
        {
            Clear();

            DateTime _currentMonth = DateContext.CurrentShownMonth;
            monthLabel.text = _currentMonth.ToString("MMMM yyyy");

            DateTime rangeStart = _currentMonth;
            DateTime rangeEnd = _currentMonth.AddMonths(1).AddDays(-1);

            var grouped = CalendarUtils.GetExpandedMonthItems(new CalendarRepository(), rangeStart, rangeEnd);
            foreach (var groupItem in grouped)
            {
                var header = Instantiate(dayHeaderPrefab, content);
                header.SetDate(groupItem.Key);
                if(groupItem.Key == DateTime.Today)
                    header.SetHighlight(true);

                foreach (var entry in groupItem.OrderBy(e => e.occurrenceStart))
                {
                    var row = Instantiate(itemRowPrefab, content);
                    row.Initialize(entry.item, entry.occurrenceStart, OnItemClicked, groupItem.Key);
                    Instantiate(paddingPrefab, content);
                }
            }
        }

        private void OnItemClicked((CalendarItem item, DateTime shownOnDate) args)
        {
            EditItemContext.EditingItemId = args.item.Id;
            SceneHistoryManager.Instance.LoadScene(AppScene.EditItem);
        }

        private void Clear()
        {
            foreach (Transform child in content)
                Destroy(child.gameObject);
        }

        public bool OnBackButtonPressed()
        {
            if (sideMenuPopover.gameObject.activeSelf)
            {
                sideMenuPopover.gameObject.SetActive(false);
                return true;
            }
            return false; // Let the manager switch scenes
        }
        
        public void Add()
        {
            // To open create/edit scene
            EditItemContext.SelectedDate = DateTime.Today;
            SceneHistoryManager.Instance.LoadScene(AppScene.EditItem);
        }

        public void OpenSideMenu()
        {
            sideMenuPopover.gameObject.SetActive(true);
        }
    }
}
