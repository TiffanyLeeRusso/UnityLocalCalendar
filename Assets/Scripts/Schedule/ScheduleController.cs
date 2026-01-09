using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.Linq;
using UnityEngine;
using LocalCalendar.Data;
using LocalCalendar.Models;
using LocalCalendar.App;
using LocalCalendar.Calendar;
using LocalCalendar.Services;

namespace LocalCalendar.Schedule
{
    public class ScheduleController : MonoBehaviour
    {
        [SerializeField] private Transform content;
        [SerializeField] private ScheduleDayHeader dayHeaderPrefab;
        [SerializeField] private DayEventRow itemRowPrefab;

        private CalendarRepository _repo;

        void Start()
        {
            _repo = new CalendarRepository();
            BuildSchedule();
        }

        private void BuildSchedule()
        {
            Clear();

            DateTime rangeStart = DateTime.Today;
            DateTime rangeEnd = rangeStart.AddDays(30);

            var items = _repo.GetAllCalendarItems();

            // (date shown in schedule, item, occurrenceStartLocal, occurrenceEndLocal)
            var expanded = new List<(DateTime date, CalendarItem item)>();

            foreach (var item in items)
            {
                /*
                 if (item.RepeatRule == null)
                 {
                     // Single occurrence
                     var local = item.StartUtc.ToLocalTime();
                     if (local.Date >= rangeStart && local.Date <= rangeEnd)
                         expanded.Add((local.Date, item));
                 }
                 else
                 {
                     foreach (var occurrence in RecurrenceExpander
                              .ExpandOccurrences(item, rangeStart, rangeEnd))
                     {
                         expanded.Add((occurrence.Date, item));
                     }
                 }
                */

                // Expand recurrence (or single occurrence if no repeat)
                foreach (var occurrenceStartUtc in
                         RecurrenceExpander.ExpandOccurrences(item, rangeStart, rangeEnd))
                {
                    DateTime occurrenceStartLocal = occurrenceStartUtc.ToLocalTime();

                    // Determine occurrence end
                    DateTime occurrenceEndLocal;
                    if (item.EndUtc > item.StartUtc)
                    {
                        // Multi-day or timed event
                        TimeSpan duration = item.EndUtc - item.StartUtc;
                        occurrenceEndLocal = occurrenceStartLocal.Add(duration);
                    }
                    else
                    {
                        // Reminder or instant event
                        occurrenceEndLocal = occurrenceStartLocal.AddMinutes(1);
                    }

                    // Clamp to visible range
                    DateTime dayCursor = occurrenceStartLocal.Date;
                    DateTime lastDay = occurrenceEndLocal.Date;

                    while (dayCursor <= lastDay)
                    {
                        if (dayCursor >= rangeStart && dayCursor <= rangeEnd)
                        {
                            // Only add if this day actually overlaps the occurrence
                            DateTime dayStart = dayCursor;
                            DateTime dayEnd = dayStart.AddDays(1);

                            bool overlaps =
                                occurrenceStartLocal < dayEnd &&
                                occurrenceEndLocal > dayStart;

                            if (overlaps)
                            {
                                expanded.Add((dayCursor, item));
                            }
                        }

                        dayCursor = dayCursor.AddDays(1);
                    }
                }
            }

            var grouped = expanded
                .GroupBy(e => e.date)
                .OrderBy(g => g.Key);

            foreach (var groupItem in grouped)
            {
                var header = Instantiate(dayHeaderPrefab, content);
                header.SetDate(groupItem.Key);
                Debug.Log(groupItem.Key.ToString("dddd, MMM d"));

                foreach (var entry in groupItem.OrderBy(
                             e => e.item.StartUtc.ToLocalTime()))
                {
                    var row = Instantiate(itemRowPrefab, content);
                    row.Initialize(entry.item, groupItem.Key, OnItemClicked);

                    // Optional future improvement:
                    // row.SetOccurrenceDate(groupItem.Key);
                }
            }
        }

        /*
        private void BuildSchedule()
        {
            Clear();

            DateTime start = DateTime.Today;
            DateTime end = start.AddDays(30);

            var items = _repo.GetAll();

            var expanded = new List<(DateTime date, CalendarItem item)>();

            foreach (var item in items)
            {
                foreach (var occurrence in RecurrenceExpander.ExpandOccurrences(item, start, end)) {
                    // Is this correct?
                    for (DateTime d = start; d < end; d = d.AddDays(1))
                    {
                        expanded.Add((d, item));
                    }

                    //expanded.Add((occurrence.Date, item));
                }
            }

            var grouped = expanded
                .GroupBy(e => e.date)
                .OrderBy(g => g.Key);

            foreach (var group in grouped)
            {
                var header = Instantiate(dayHeaderPrefab, content);
                header.SetDate(group.Key);

                foreach (var entry in group.OrderBy(
                             e => e.item.StartUtc.ToLocalTime()))
                {
                    var row = Instantiate(itemRowPrefab, content);
                    row.Initialize(entry.item, OnItemClicked);

                    // Optional: tell row this is a recurring occurrence
                    //row.SetOccurrenceDate(group.Key);
                }
            }
        }
        */

        private void OnItemClicked(CalendarItem item)
        {
            EditItemContext.EditingItemId = item.Id;
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

        public void Back()
        {
            SceneManager.LoadScene("CalendarScene");
        }
    }
}
