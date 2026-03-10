using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using LocalCalendar.Data;
using LocalCalendar.Services;
using LocalCalendar.Utils;
using LocalCalendar.Prefabs;

namespace LocalCalendar.Controllers
{
    public class ScheduleController : MonoBehaviour
    {
        [SerializeField] private Header header;
        [SerializeField] private Transform content;
        [SerializeField] private ScheduleDayHeader dayHeaderPrefab;
        [SerializeField] private DayEventRow itemRowPrefab;
        [SerializeField] private RectTransform paddingPrefab;
        [SerializeField] private SidePanelPopover sideMenuPopover;
        [SerializeField] GameObject catObj;

        void OnEnable()
        {
            if (CalendarRefreshSignal.NeedsRefresh)
            {
                CalendarRefreshSignal.NeedsRefresh = false;
                BuildSchedule();
            }
        }

        void Start()
        {
            header.Configure(new HeaderConfig{ SideMenuPopover = sideMenuPopover,
                                               SceneTitle = "Schedule"});
            header.OnPrev += PrevMonth;
            header.OnNext += NextMonth;

            catObj.SetActive(SettingsService.GetCatsActive());

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
            DateTime firstDay = new DateTime(_currentMonth.Year,
                                             _currentMonth.Month,
                                             1);

            header.title.text = _currentMonth.ToString("MMMM yyyy");
            header.currentDate = (_currentMonth.Year == DateTime.Today.Year && _currentMonth.Month == DateTime.Today.Month) ?
                DateTime.Today :
                firstDay; // Today if we are on the current month; otherwise, the first of the month
            
            DateTime rangeStart = _currentMonth;
            DateTime rangeEnd = _currentMonth.AddMonths(1).AddDays(-1);

            var grouped = CalendarUtils
                .GetExpandedMonthItems(new CalendarRepository(), rangeStart, rangeEnd)
                .ToList();

            DateTime today = DateTime.Today;

            // Ensure today exists even with no events
            if (today >= rangeStart && today <= rangeEnd &&
                !grouped.Any(g => g.Key.Date == today))
            {
                grouped.Add(
                    new[] { (date: today, item: (CalendarItem)null, occurrenceStart: today) }
                    .GroupBy(x => x.date)
                    .First()
                );
            }

            foreach (var groupItem in grouped.OrderBy(g => g.Key))
            {
                var header = Instantiate(dayHeaderPrefab, content);
                header.SetDate(groupItem.Key);

                if (groupItem.Key == today)
                    header.SetHighlight(true);

                foreach (var entry in groupItem
                         .Where(e => e.item != null)
                         .OrderBy(e => e.occurrenceStart))
                {
                    var row = Instantiate(itemRowPrefab, content);
                    row.Initialize(entry.item, entry.occurrenceStart, OnItemClicked, groupItem.Key);
                    var visuals = row.GetComponent<CalendarItemVisuals>();
                    visuals.ApplyStyle(entry.item.Color);
                    Instantiate(paddingPrefab, content);
                }
            }
        }

        private void OnItemClicked((CalendarItem item, DateTime shownOnDate) args)
        {
            EditItemContext.EditingItemId = args.item.Id;
            EditItemContext.Mode = EditItemMode.Preview;
            SceneHistoryManager.Instance.LoadScene(AppScene.EditItem);
        }

        private void Clear()
        {
            foreach (Transform child in content)
                Destroy(child.gameObject);
        }
    }
}
