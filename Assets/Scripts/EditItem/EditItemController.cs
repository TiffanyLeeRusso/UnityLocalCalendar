using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using LocalCalendar.Data;
using LocalCalendar.Notifications;
using LocalCalendar.Services;
using LocalCalendar.Calendar;

namespace LocalCalendar.EditItem
{
    public class EditItemController : MonoBehaviour
    {
        [SerializeField] private TMP_InputField titleInput;
        [SerializeField] private GameObject titleDecoration;
        [SerializeField] private TMP_InputField noteInput;
        [SerializeField] private GameObject noteDecoration;

        [SerializeField] private Toggle allDayToggle;
        [SerializeField] private GameObject nowBtn;
        [SerializeField] private TMP_Text startDateText;
        [SerializeField] private DatePicker startDatePicker;
        [SerializeField] private TMP_Text endDateText;
        [SerializeField] private DatePicker endDatePicker;

        [SerializeField] private TMP_Text timeText;
        [SerializeField] private GameObject timePickerContainer;
        [SerializeField] private TimePicker startTimePicker;
        [SerializeField] private TimePicker endTimePicker;

        [SerializeField] private TMP_Text reminderText;
        [SerializeField] private Toggle reminderToggle;
        [SerializeField] private GameObject reminderContainer;
        [SerializeField] private TMP_Dropdown reminderDropdown;

        [SerializeField] private TMP_Text repeatText;
        [SerializeField] private Toggle repeatToggle;
        [SerializeField] private TMP_Dropdown repeatUnitDropdown;
        [SerializeField] private TMP_InputField repeatIntervalInput;
        [SerializeField] private TMP_InputField repeatUntilInput;
        [SerializeField] private GameObject repeatContainer;

        [SerializeField] private GameObject editBtns;
        [SerializeField] private GameObject previewBtns;
        [SerializeField] private GameObject deleteButton;

        private CalendarRepository _repo;
        private CalendarItem _item;
        private bool _suppressTimeEvents;
        private TimeSpan _lastDuration = TimeSpan.FromHours(1);
        private DateTime _startDate;
        private DateTime _endDate;

        static readonly TimeSpan[] ReminderOffsets =
        {
            TimeSpan.Zero,
            TimeSpan.FromMinutes(10),
            TimeSpan.FromHours(1),
            TimeSpan.FromDays(1)
        };

        void Start()
        {
            _repo = new CalendarRepository();

            // Change events
            startTimePicker.OnTimeChanged += OnStartTimeChanged;
            endTimePicker.OnTimeChanged += OnEndTimeChanged;
            startDatePicker.OnDateChanged += OnStartDateChanged;
            endDatePicker.OnDateChanged += OnEndDateChanged;
            allDayToggle.onValueChanged.AddListener(OnAllDayChanged);
            reminderToggle.onValueChanged.AddListener(_ => reminderDropdown.gameObject.SetActive(reminderToggle.isOn));
            repeatToggle.onValueChanged.AddListener(_ => repeatContainer.SetActive(repeatToggle.isOn));

            // Add vs edit
            bool isEditing = !string.IsNullOrEmpty(EditItemContext.EditingItemId);
            deleteButton.SetActive(isEditing);

            PopulateReminderDropdown();
            PopulateRepeatDropdown();
            LoadContext();
        }

        void OnDestroy()
        {
            EditItemContext.Clear();
        }

        private void RefreshDateText()
        {
            startDateText.text = _startDate.ToString("yyyy-MM-dd");
            endDateText.text = _endDate.ToString("yyyy-MM-dd");
        }

        private void OnStartDateChanged(DateTime newStartDate)
        {
            _startDate = newStartDate.Date;

            // If start moved after end, push end forward
            if (_endDate < _startDate)
              _endDate = _startDate;

            endDatePicker.SetDate(_endDate);
            RefreshDateText();
        }

        private void OnEndDateChanged(DateTime newEndDate)
        {
            _endDate = newEndDate.Date;

            // End cannot be before start
            if (_endDate < _startDate)
              _endDate = _startDate;

            endDatePicker.SetDate(_endDate);
            RefreshDateText();
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
            RefreshDateText();

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
                timePickerContainer.SetActive(true);
                // Normalize to all-day boundaries
                // Note the date part does not matter since this is just a *time* picker.
                startTimePicker.SetTime(
                    new DateTime(1, 1, 1, 0, 0, 0)); // 12:00 AM
                endTimePicker.SetTime(
                    new DateTime(1, 1, 1, 0, 0, 0));
            }
            else
                timePickerContainer.SetActive(false);
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

            RefreshDateText();

            _suppressTimeEvents = false;
        }

        public void OnSavePressed()
        {
            try
            {
                var item = BuildItemFromUI();
                _repo.Save(item);
                NotificationScheduler.Schedule(item);

                CalendarRefreshSignal.NeedsRefresh = true;
                SceneHistoryManager.Instance.GoBack();
            }
            catch (Exception ex)
            {
                Debug.LogError("Failed to save item: " + ex);
                LoggingService.Warn(LogCategory.DB, "Failed to save item: " + ex);
            }
        }

        public void OnCancelPressed()
        {
            SceneHistoryManager.Instance.GoBack();
        }

        public void DeleteItem()
        {
            if (string.IsNullOrEmpty(EditItemContext.EditingItemId))
                return;

            NotificationScheduler.Cancel(_item);
            _repo.Delete(EditItemContext.EditingItemId);

            CalendarRefreshSignal.NeedsRefresh = true;
            SceneHistoryManager.Instance.GoBack();
        }

        public void OnEditPressed()
        {
            EditItemContext.EditingItemId = _item.Id;
            SceneHistoryManager.Instance.LoadScene(AppScene.EditItem);
        }

        void BuildUIFromItem()
        {
            // Title & Note
            titleInput.text = _item.Title;
            noteInput.text = _item.Note;
            if (EditItemContext.Mode == EditItemMode.Preview)
            {
                titleInput.readOnly = true;
                noteInput.readOnly = true;
                titleDecoration.SetActive(false);
                noteDecoration.SetActive(false);
                titleInput.gameObject.SetActive(titleInput.text.Length > 0);
                noteInput.gameObject.SetActive(noteInput.text.Length > 0);
            }
            else
            {
                titleInput.readOnly = false;
                noteInput.readOnly = false;
                titleDecoration.SetActive(true);
                noteDecoration.SetActive(true);
                titleInput.gameObject.SetActive(true);
                noteInput.gameObject.SetActive(true);
            }

            // All Day/Date/time
            RefreshDateText();
            bool allDay = _item.AllDay;
            var startLocal = _item.StartUtc.ToLocalTime();
            var endLocal = _item.EndUtc.ToLocalTime();
            if (EditItemContext.Mode == EditItemMode.Edit)
            {
                // All day & time
                allDayToggle.isOn = allDay;
                allDayToggle.gameObject.SetActive(true);
                timePickerContainer.SetActive(!allDay);
                timeText.gameObject.SetActive(false);
                if (!allDay)
                {
                    startTimePicker.SetTime(startLocal);
                    endTimePicker.SetTime(endLocal);
                }

                // Date
                startDatePicker.SetDate(_startDate);
                endDatePicker.SetDate(_endDate);
                _lastDuration = endLocal - startLocal;
            }
            else // preview mode
            {
                allDayToggle.gameObject.SetActive(false);
                timePickerContainer.SetActive(false);
                timeText.gameObject.SetActive(true);
                timeText.text = allDay ? "All day" : $"{startLocal:hh:mm tt} – {endLocal:hh:mm tt}";
            }

            nowBtn.SetActive(EditItemContext.Mode == EditItemMode.Edit);
            startDatePicker.gameObject.SetActive(EditItemContext.Mode == EditItemMode.Edit);
            endDatePicker.gameObject.SetActive(EditItemContext.Mode == EditItemMode.Edit);

            // Reminder
            bool isReminder = _item.Reminder != null;
            if (EditItemContext.Mode == EditItemMode.Edit)
            {
                // Set the reminder toggle.
                // It is always visible in add/edit mode
                reminderToggle.isOn = isReminder;
                reminderToggle.gameObject.SetActive(true);
                // The reminder options are visible only when the toggle is checked.
                reminderContainer.gameObject.SetActive(isReminder);
                if (isReminder)
                {
                    reminderDropdown.value = Array.IndexOf(ReminderOffsets, _item.Reminder.Offset);
                    if (reminderDropdown.value < 0)
                        reminderDropdown.value = 0;

                    reminderDropdown.RefreshShownValue();
                }
            }
            else
            {
                // Preview mode
                reminderToggle.gameObject.SetActive(false);
                reminderContainer.SetActive(false);
                if(isReminder)
                {
                    reminderText.gameObject.SetActive(true);
                    reminderText.text = DataFormatter.ToString(_item.Reminder);
                }
                else
                    reminderText.gameObject.SetActive(false);
            }
            
            // Repeat
            bool repeats = _item.RepeatRule != null;
            if (EditItemContext.Mode == EditItemMode.Edit)
            {
                repeatToggle.gameObject.SetActive(true);
                repeatText.gameObject.SetActive(false);
                if (repeats)
                {
                    repeatContainer.SetActive(true);
                    repeatToggle.isOn = true;
                    repeatIntervalInput.text = (_item.RepeatRule.Interval).ToString();
                    repeatUnitDropdown.value = (int)_item.RepeatRule.Unit;

                    repeatUntilInput.text = _item.RepeatRule.UntilUtc.HasValue
                        ? _item.RepeatRule.UntilUtc.Value.ToLocalTime().ToString("yyyy-MM-dd")
                        : "";
                }
                else
                {
                    repeatToggle.isOn = false;
                    repeatContainer.SetActive(false);
                }
            }
            else
            {
                // Preview mode. Just print the text.
                repeatToggle.gameObject.SetActive(false);
                repeatContainer.SetActive(false);
                repeatText.gameObject.SetActive(repeats);
                repeatText.text = repeats ? DataFormatter.ToString(_item.RepeatRule) : "";
            }

            // Buttons
            previewBtns.SetActive(EditItemContext.Mode == EditItemMode.Preview);
            editBtns.SetActive(EditItemContext.Mode == EditItemMode.Edit);
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
                    Offset = ReminderOffsets[reminderDropdown.value]
                };
            }

            return item;
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
