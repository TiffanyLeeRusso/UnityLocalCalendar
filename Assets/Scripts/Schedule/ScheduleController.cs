using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using LocalCalendar.Data;
using LocalCalendar.Calendar;
using LocalCalendar.Services;
using LocalCalendar.EditItem;

namespace LocalCalendar.Schedule
{
    public class ScheduleController : MonoBehaviour
    {
        [SerializeField] private TMP_Text monthLabel;
        [SerializeField] private Transform content;
        [SerializeField] private ScheduleDayHeader dayHeaderPrefab;
        [SerializeField] private DayEventRow itemRowPrefab;
        [SerializeField] private RectTransform paddingPrefab;
        [SerializeField] private SidePanelPopover sideMenuPopover;

        private CalendarRepository _repo;
        //private DateTime _currentMonth;

        void Start()
        {
            _repo = new CalendarRepository();
            BuildSchedule();
        }

        void OnEnable()
        {
            if (CalendarRefreshSignal.NeedsRefresh)
            {
                CalendarRefreshSignal.NeedsRefresh = false;
                BuildSchedule();
            }
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

            var grouped = CalendarUtils.GetExpandedMonthItems(_repo, rangeStart, rangeEnd);
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

        public void OpenSideMenu()
        {
            sideMenuPopover.gameObject.SetActive(true);
        }
    }
}
