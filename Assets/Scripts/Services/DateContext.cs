using System;

namespace LocalCalendar.Services
{
    public static class DateContext
    {
        private static DateTime? _currentShownMonth;

        // Gets or sets the currently shown month.
        // Always normalized to the first day of that month.
        // If unset, returns today's month.
        public static DateTime CurrentShownMonth
        {
            get
            {
                if (_currentShownMonth == null)
                {
                    var today = DateTime.Today;
                    return new DateTime(today.Year, today.Month, 1);
                }

                return _currentShownMonth.Value;
            }
            set
            {
                _currentShownMonth = new DateTime(value.Year, value.Month, 1);
            }
        }

        public static void Today()
        {
            CurrentShownMonth = DateTime.Today;
        }

        public static void NextMonth()
        {
            CurrentShownMonth = CurrentShownMonth.AddMonths(1);
        }

        public static void PrevMonth()
        {
            CurrentShownMonth = CurrentShownMonth.AddMonths(-1);
        }

        public static void Clear()
        {
            _currentShownMonth = null;
        }
    }
}
