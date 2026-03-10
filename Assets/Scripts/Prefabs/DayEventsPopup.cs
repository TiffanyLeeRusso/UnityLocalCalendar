using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using LocalCalendar.Data;
using LocalCalendar.Services;
using LocalCalendar.Utils;

namespace LocalCalendar.Prefabs
{
    public class DayEventsPopup : MonoBehaviour
    {
        [SerializeField] private Header header;
        [SerializeField] private Transform content;
        [SerializeField] private DayEventRow rowPrefab;

        private DateTime _currentDate;
        private CalendarRepository _repo;

        void Awake()
        {
            _repo = new CalendarRepository();
        }

        void Start()
        {
            header.Configure(new HeaderConfig{ ShowBack = true,
                                               ShowSidePanel = false,
                                               SceneTitle = "Day View" });
            header.OnBack += Hide;
            header.OnPrev += PrevDay;
            header.OnNext += NextDay;
        }

        public void Show(DateTime date)
        {
            gameObject.SetActive(true);
            SetDate(date);
        }

        public void PrevDay()
        {
            SetDate(_currentDate.AddDays(-1));
        }

        public void NextDay()
        {
            SetDate(_currentDate.AddDays(1));
        }

        private void SetDate(DateTime date)
        {
            _currentDate = date.Date;
            header.title.text = _currentDate.ToString("ddd, MMM d, yyyy");
            header.currentDate = _currentDate;
            LoadEvents();
        }

        private void LoadEvents()
        {
            Clear();

            var dayItems = CalendarUtils.GetExpandedDayItems(_repo, _currentDate);
            foreach(var item in dayItems)
            {
                var row = Instantiate(rowPrefab, content);
                row.Initialize(item.item, item.occurrenceStart, OnItemClicked, _currentDate);
                var visuals = row.GetComponent<CalendarItemVisuals>();
                visuals.ApplyStyle(item.item.Color);
            }
        }

        private void OnItemClicked((CalendarItem item, DateTime shownOnDate) args)
        {
            EditItemContext.Clear();
            EditItemContext.EditingItemId = args.item.Id;
            EditItemContext.Mode = EditItemMode.Preview;
            SceneHistoryManager.Instance.LoadScene(AppScene.EditItem);
        }
        
        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void Clear()
        {
            foreach (Transform child in content)
                Destroy(child.gameObject);
        }
    }
}
