using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using LocalCalendar.Data;
using LocalCalendar.EditItem;
using LocalCalendar.Services;

namespace LocalCalendar.Calendar
{
    public class DayEventsPopup : MonoBehaviour
    {
        [SerializeField] private TMP_InputField dateLabel;
        [SerializeField] private Transform content;
        [SerializeField] private DayEventRow rowPrefab;

        private DateTime _currentDate;
        private CalendarRepository _repo;

        void Awake()
        {
            _repo = new CalendarRepository();
        }

        public void Show(DateTime date)
        {
            gameObject.SetActive(true);
            SetDate(date);
        }

        public void OnPrevDay()
        {
            SetDate(_currentDate.AddDays(-1));
        }

        public void OnNextDay()
        {
            SetDate(_currentDate.AddDays(1));
        }

        private void SetDate(DateTime date)
        {
            _currentDate = date.Date;
            dateLabel.text = _currentDate.ToString("dddd, MMM d");
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
            }
        }

        public void Add()
        {
            // To open create/edit scene
            EditItemContext.SelectedDate = _currentDate;
            SceneHistoryManager.Instance.LoadScene(AppScene.EditItem);
        }

        private void OnItemClicked((CalendarItem item, DateTime shownOnDate) args)
        {
            EditItemContext.EditingItemId = args.item.Id;
            EditItemContext.SelectedDate = null;
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
