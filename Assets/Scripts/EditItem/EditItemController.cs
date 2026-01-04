using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;
using LocalCalendar.Data;
using LocalCalendar.Models;
using LocalCalendar.App;
using LocalCalendar.Notifications;

namespace LocalCalendar.EditItem
{
    public class EditItemController : MonoBehaviour
    {
        public Toggle isReminder;

        public TMP_InputField titleInput;
        public TMP_InputField noteInput;
        public TMP_InputField dateInput;
        //public TMP_InputField timeInput;
        [SerializeField] private GameObject timePickerContainer;
        [SerializeField] private TimePicker startTimePicker;
        [SerializeField] private TimePicker endTimePicker;
        [SerializeField] private Toggle allDayToggle;

        public GameObject reminderRow;
        public TMP_Dropdown reminderDropdown;

        [SerializeField] private GameObject deleteButton;
    
        private CalendarRepository _repo;
        private CalendarItem _item;

        void Start()
        {
            _repo = new CalendarRepository();

            allDayToggle.onValueChanged.AddListener(OnAllDayChanged);
            isReminder.onValueChanged.AddListener(_ => UpdateReminderVisibility());
        
            bool isEditing = !string.IsNullOrEmpty(EditItemContext.EditingItemId);
            deleteButton.SetActive(isEditing);
        
            PopulateReminderDropdown();
            UpdateReminderVisibility();
            LoadContext();
        }

        private void OnAllDayChanged(bool isAllDay)
        {
            timePickerContainer.SetActive(!isAllDay);

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

        void UpdateReminderVisibility()
        {
            reminderRow.SetActive(isReminder.isOn);
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
                BuildUIFromItem();
            }
            else
            {
                // New item
                DateTime baseDate = EditItemContext.SelectedDate ?? DateTime.Today;
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

        public void OnSavePressed()
        {
            try
            {
                var item = BuildItemFromUI();
                _repo.Save(item);
                NotificationScheduler.Cancel(_item); // cancel old if editing
                NotificationScheduler.Schedule(item);

                EditItemContext.Clear();
                CalendarRefreshSignal.NeedsRefresh = true;
                SceneManager.LoadScene("CalendarScene");
            }
            catch (Exception ex)
            {
                Debug.LogError("Failed to save item: " + ex);
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

            CalendarRefreshSignal.NeedsRefresh = true;
            SceneManager.LoadScene("CalendarScene");
        }

        void BuildUIFromItem()
        {
            titleInput.text = _item.Title;
            noteInput.text = _item.Note;

            var startLocal = _item.StartUtc.ToLocalTime();
            var endLocal = _item.EndUtc.ToLocalTime();

            dateInput.text = startLocal.ToString("yyyy-MM-dd");

            allDayToggle.isOn = _item.AllDay;
            timePickerContainer.SetActive(!_item.AllDay);

            if (!_item.AllDay)
            {
                startTimePicker.SetTime(startLocal);
                endTimePicker.SetTime(endLocal);
            }

            isReminder.isOn = _item.Type == CalendarItemType.Reminder;
            //UpdateReminderVisibility();
            // TODO: the rest
            /*    public class CalendarItem
                  {
                  public string Id;
                  public CalendarItemType Type;
              
                  public string Title;
                  public string Note;

                  public DateTime StartUtc;
                  public DateTime EndUtc;

                  public bool AllDay;

                  public RepeatRule RepeatRule;
                  public ReminderSettings Reminder;
                  }
            */

        }

        CalendarItem BuildItemFromUI()
        {
            DateTime date = DateTime.Parse(dateInput.text);

            DateTime startLocal;
            DateTime endLocal;

            if (allDayToggle.isOn)
            {
                startLocal = date.Date;
                endLocal = date.Date.AddDays(1);
            }
            else
            {
                startLocal = date.Date + startTimePicker.GetTime();
                endLocal = date.Date + endTimePicker.GetTime();

                // Safety: prevent inverted times
                if (endLocal <= startLocal)
                    endLocal = startLocal.AddMinutes(5);
            }

            var item = new CalendarItem
            {
                Id = _item?.Id ?? Guid.NewGuid().ToString(),
                Type = isReminder.isOn
                    ? CalendarItemType.Reminder
                    : CalendarItemType.Event,

                Title = titleInput.text,
                Note = noteInput.text,

                StartUtc = startLocal.ToUniversalTime(),
                EndUtc = endLocal.ToUniversalTime(),
                AllDay = allDayToggle.isOn
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
        /*
        CalendarItem BuildItemFromUI()
        {
            //DateTime localDateTime = DateTime.Parse(
            //    $"{dateInput.text} {timeInput.text}");
            //DateTime startUtc = localDateTime.ToUniversalTime();

            DateTime date = DateTime.Parse($"{dateInput.text}");

            TimeSpan startTime = startTimePicker.GetTime();
            TimeSpan endTime = endTimePicker.GetTime();

            DateTime startLocal = date + startTime;
            DateTime endLocal = date + endTime;
            
            var item = new CalendarItem
            {
                Id = _item?.Id ?? Guid.NewGuid().ToString(),
                Type = isReminder.isOn
                ? CalendarItemType.Reminder
                : CalendarItemType.Event,

                Title = titleInput.text,
                Note = noteInput.text,

                StartUtc = startLocal.ToUniversalTime(),
                EndUtc = endLocal.ToUniversalTime(),
                AllDay = allDayToggle.isOn
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
        */

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
    }
}
