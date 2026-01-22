using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using LocalCalendar.Data;
using LocalCalendar.Notifications;
using LocalCalendar.Services;
using LocalCalendar.Calendar;

namespace LocalCalendar.EditItem
{
    public class EditItemController : MonoBehaviour
    {
        public TMP_InputField titleInput;
        public TMP_InputField noteInput;

        [SerializeField] private DatePicker startDatePicker;
        [SerializeField] private DatePicker endDatePicker;
        [SerializeField] private TMP_Text startDateLabel;
        [SerializeField] private TMP_Text endDateLabel;
        [SerializeField] private GameObject timePickerContainer;
        [SerializeField] private TimePicker startTimePicker;
        [SerializeField] private TimePicker endTimePicker;
        [SerializeField] private Toggle allDayToggle;
        [SerializeField] private Toggle repeatToggle;
        [SerializeField] private Toggle reminderToggle;
        [SerializeField] private TMP_Dropdown repeatUnitDropdown;
        [SerializeField] private TMP_InputField repeatIntervalInput;
        [SerializeField] private TMP_InputField repeatUntilInput;
        [SerializeField] private GameObject repeatContainer;

        public GameObject reminderRow;
        public TMP_Dropdown reminderDropdown;

        [SerializeField] private GameObject deleteButton;

        private CalendarRepository _repo;
        private CalendarItem _item;
        private bool _suppressTimeEvents;
        private TimeSpan _lastDuration = TimeSpan.FromHours(1);
        private DateTime _startDate;
        private DateTime _endDate;

        void Start()
        {
            _repo = new CalendarRepository();

            // Change events
            startTimePicker.OnTimeChanged += OnStartTimeChanged;
            endTimePicker.OnTimeChanged += OnEndTimeChanged;
            startDatePicker.OnDateChanged += OnStartDateChanged;
            endDatePicker.OnDateChanged += OnEndDateChanged;
            allDayToggle.onValueChanged.AddListener(OnAllDayChanged);
            allDayToggle.onValueChanged.AddListener(_ => RefreshVisibility());
            reminderToggle.onValueChanged.AddListener(_ => RefreshVisibility());
            repeatToggle.onValueChanged.AddListener(_ => RefreshVisibility());

            bool isEditing = !string.IsNullOrEmpty(EditItemContext.EditingItemId);
            deleteButton.SetActive(isEditing);

            PopulateReminderDropdown();
            PopulateRepeatDropdown();
            RefreshVisibility();
            LoadContext();
        }

        void RefreshVisibility()
        {
            bool reminder = reminderToggle.isOn;
            bool repetitive = repeatToggle.isOn;
            bool allDay = allDayToggle.isOn;

            endTimePicker.gameObject.SetActive(!allDay);
            startTimePicker.gameObject.SetActive(!allDay);

            reminderRow.SetActive(reminder);
            repeatContainer.SetActive(repetitive);
        }

        private void RefreshDateLabels()
        {
            startDateLabel.text = _startDate.ToString("yyyy-MM-dd");
            endDateLabel.text = _endDate.ToString("yyyy-MM-dd");
        }

        private void OnStartDateChanged(DateTime newStartDate)
        {
            _startDate = newStartDate.Date;

            // If start moved after end, push end forward
            if (_endDate < _startDate)
              _endDate = _startDate;

            endDatePicker.SetDate(_endDate);
            RefreshDateLabels();
        }

        private void OnEndDateChanged(DateTime newEndDate)
        {
            _endDate = newEndDate.Date;

            // End cannot be before start
            if (_endDate < _startDate)
              _endDate = _startDate;

            endDatePicker.SetDate(_endDate);
            RefreshDateLabels();
        }

        private void OnStartTimeChanged(DateTime newStart)
        {
            if (_suppressTimeEvents) return;

            _suppressTimeEvents = true;

            var newEnd = newStart + _lastDuration;
            if (newEnd.Date > _endDate)
            {
                _endDate = newEnd.Date;
                endDatePicker.SetDate(_endDate);
            }

            endTimePicker.SetTime(newEnd);
            RefreshDateLabels();

            _suppressTimeEvents = false;
        }

        private void OnEndTimeChanged(DateTime newEnd)
        {
            if (_suppressTimeEvents) return;

            var start = GetStartDateTime();
            var duration = newEnd - start;

            if (duration > TimeSpan.Zero)
            {
              _lastDuration = duration;
            }
            else
            {
                _lastDuration = TimeSpan.FromHours(1);
            }
        }

        private DateTime GetStartDateTime()
        {
            return _startDate + startTimePicker.GetTime();
        }

        private DateTime GetEndDateTime()
        {
            return _endDate + endTimePicker.GetTime();
        }

        private void OnAllDayChanged(bool isAllDay)
        {
            if (isAllDay)
            {
                // Normalize to all-day boundaries
                // Note the date part does not matter since this is just a *time* picker.
                startTimePicker.SetTime(
                    new DateTime(1, 1, 1, 0, 0, 0)); // 12:00 AM
                endTimePicker.SetTime(
                    new DateTime(1, 1, 1, 0, 0, 0));
            }
        }

        void PopulateRepeatDropdown()
        {
            repeatUnitDropdown.ClearOptions();
            repeatUnitDropdown.AddOptions(new System.Collections.Generic.List<string>
            {
                "Day",
                "Week",
                "Month",
                "Year"
            });
        }
        
        void PopulateReminderDropdown()
        {
            reminderDropdown.ClearOptions();
            reminderDropdown.AddOptions(new System.Collections.Generic.List<string>
            {
                "At time",
                "10 minutes before",
                "1 hour before",
                "1 day before"
            });
        }

        void LoadContext()
        {
            if (!string.IsNullOrEmpty(EditItemContext.EditingItemId))
            {
                // Load item
                _item = _repo.GetById(EditItemContext.EditingItemId);
                var startLocal = _item.StartUtc.ToLocalTime();
                var endLocal = _item.EndUtc.ToLocalTime();

                _startDate = startLocal.Date;
                _endDate = endLocal.Date;
                _lastDuration = endLocal - startLocal;

                BuildUIFromItem();
            }
            else
            {
                // New item
                DateTime baseDate = EditItemContext.SelectedDate ?? DateTime.Today;
                _startDate = baseDate;
                _endDate = baseDate;
                _item = new CalendarItem
                {
                    Id = Guid.NewGuid().ToString(),
                    StartUtc = baseDate.AddHours(9).ToUniversalTime(), // 9am
                    EndUtc = baseDate.AddHours(10).ToUniversalTime(),
                    Type = CalendarItemType.Event
                };
                BuildUIFromItem();
            }
        }

        public void OnNowPressed()
        {
            _suppressTimeEvents = true;

            // Ensure time-based mode
            allDayToggle.isOn = false;

            DateTime now = DateTime.Now;

            // Round UP to next whole hour
            DateTime start = new DateTime(
                now.Year,
                now.Month,
                now.Day,
                now.Hour,
                0,
                0
            ).AddHours(1);

            DateTime end = start + _lastDuration;

            // Update dates
            _startDate = start.Date;
            _endDate = end.Date;

            startDatePicker.SetDate(_startDate);
            endDatePicker.SetDate(_endDate);

            // Update times
            startTimePicker.SetTime(start);
            endTimePicker.SetTime(end);

            RefreshDateLabels();

            _suppressTimeEvents = false;
        }

        public void OnSavePressed()
        {
            try
            {
                var item = BuildItemFromUI();
                _repo.Save(item);
                NotificationScheduler.Schedule(item);

                EditItemContext.Clear();
                CalendarRefreshSignal.NeedsRefresh = true;
                SceneManager.LoadScene("CalendarScene");
            }
            catch (Exception ex)
            {
                Debug.LogError("Failed to save item: " + ex);
                LoggingService.Warn(LogCategory.DB, "Failed to save item: " + ex);
            }
        }

        public void OnCancelPressed()
        {
            EditItemContext.Clear();
            SceneManager.LoadScene("CalendarScene");
        }

        public void DeleteItem()
        {
            if (string.IsNullOrEmpty(EditItemContext.EditingItemId))
                return;

            NotificationScheduler.Cancel(_item);
            _repo.Delete(EditItemContext.EditingItemId);
            EditItemContext.Clear();

            CalendarRefreshSignal.NeedsRefresh = true;
            SceneManager.LoadScene("CalendarScene");
        }

        void BuildUIFromItem()
        {
            // Title & Note
            titleInput.text = _item.Title;
            noteInput.text = _item.Note;

            // Date
            var startLocal = _item.StartUtc.ToLocalTime();
            var endLocal = _item.EndUtc.ToLocalTime();
            startDatePicker.SetDate(_startDate);
            endDatePicker.SetDate(_endDate);
            RefreshDateLabels();
            _lastDuration = endLocal - startLocal;

            // All Day
            allDayToggle.isOn = _item.AllDay;
            timePickerContainer.SetActive(!_item.AllDay);

            if (!_item.AllDay)
            {
                startTimePicker.SetTime(startLocal);
                endTimePicker.SetTime(endLocal);
            }

            // Reminder
            reminderToggle.isOn = _item.Type == CalendarItemType.Reminder;

            // Repeat
            if (_item.RepeatRule != null)
            {
                repeatToggle.isOn = true;
                repeatIntervalInput.text = (_item.RepeatRule.Interval).ToString();
                repeatUnitDropdown.value = (int)_item.RepeatRule.Unit;

                repeatUntilInput.text = _item.RepeatRule.UntilUtc.HasValue
                    ? _item.RepeatRule.UntilUtc.Value.ToLocalTime()
                    .ToString("yyyy-MM-dd")
                    : "";
            }
            else
            {
                repeatToggle.isOn = false;
            }
        }

        CalendarItem BuildItemFromUI()
        {
            DateTime startLocal;
            DateTime endLocal;

            if (allDayToggle.isOn)
            {
                startLocal = _startDate;
                endLocal = _endDate.AddDays(1);
            }
            else
            {
                startLocal = _startDate + startTimePicker.GetTime();
                endLocal = _endDate + endTimePicker.GetTime();

                // Prevent inverted times
                if (endLocal <= startLocal)
                    endLocal = startLocal.AddMinutes(5);
            }

            var item = new CalendarItem
            {
                Id = _item?.Id ?? Guid.NewGuid().ToString(),
                Type = reminderToggle.isOn
                    ? CalendarItemType.Reminder
                    : CalendarItemType.Event,

                Title = titleInput.text,
                Note = noteInput.text,

                StartUtc = startLocal.ToUniversalTime(),
                EndUtc = endLocal.ToUniversalTime(),
                AllDay = allDayToggle.isOn,
                RepeatRule = BuildRepeatRule()
            };

            if (item.Type == CalendarItemType.Reminder)
            {
                item.Reminder = new ReminderSettings
                {
                    Offset = GetReminderOffset()
                };
            }

            return item;
        }

        TimeSpan GetReminderOffset()
        {
            return reminderDropdown.value switch
            {
                0 => TimeSpan.Zero,
                1 => TimeSpan.FromMinutes(-10),
                2 => TimeSpan.FromHours(-1),
                3 => TimeSpan.FromDays(-1),
                _ => TimeSpan.Zero
            };
        }

        RepeatRule BuildRepeatRule()
        {
            if (!repeatToggle.isOn)
                return null;

            DateTime? until = null;
            if (!string.IsNullOrEmpty(repeatUntilInput.text))
                until = DateTime.Parse(repeatUntilInput.text).ToUniversalTime();

            return new RepeatRule
            {
                Interval = int.Parse(repeatIntervalInput.text),
                Unit = (RepeatUnit)repeatUnitDropdown.value,
                UntilUtc = until
            };
        }
    }
}
