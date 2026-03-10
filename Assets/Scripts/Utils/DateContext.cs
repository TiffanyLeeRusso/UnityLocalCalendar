using System;

namespace LocalCalendar.Utils
{
    public static class DateContext
    {
        private static DateTime? _currentShownYear;
        private static DateTime? _currentShownMonth;
        private static DateTime? _currentShownDay;

        // --- Month ---

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
                var normalized = new DateTime(value.Year, value.Month, 1);
                _currentShownMonth = normalized;

                // Keep day in range of this month
                if (_currentShownDay == null ||
                    _currentShownDay.Value.Year != normalized.Year ||
                    _currentShownDay.Value.Month != normalized.Month)
                {
                    _currentShownDay = normalized;
                }
            }
        }

        // --- Day ---

        // Gets or sets the currently shown day.
        // Always normalized to date only (no time).
        // If unset, returns today.
        public static DateTime CurrentShownDay
        {
            get
            {
                if (_currentShownDay == null)
                    return DateTime.Today;

                return _currentShownDay.Value.Date;
            }
            set
            {
                var normalized = value.Date;
                _currentShownDay = normalized;

                // Sync month with day
                _currentShownMonth = new DateTime(normalized.Year, normalized.Month, 1);
            }
        }

        // --- Year ---

        // Gets or sets the currently shown year.
        // If unset, returns today's year.
        public static DateTime CurrentShownYear
        {
            get
            {
                if (_currentShownYear == null)
                {
                    var today = DateTime.Today;
                    return new DateTime(today.Year, 1, 1);
                }

                return new DateTime(_currentShownYear.Value.Year, 1, 1);
            }
            set
            {
                Clear();
                _currentShownYear = new DateTime(value.Year, 1, 1);
            }
        }
        
        // --- Navigation ---

        public static void Today()
        {
            var today = DateTime.Today;
            _currentShownDay = today;
            _currentShownMonth = new DateTime(today.Year, today.Month, 1);
        }

        public static void NextMonth()
        {
            CurrentShownMonth = CurrentShownMonth.AddMonths(1);
        }

        public static void PrevMonth()
        {
            CurrentShownMonth = CurrentShownMonth.AddMonths(-1);
        }

        public static void NextDay()
        {
            CurrentShownDay = CurrentShownDay.AddDays(1);
        }

        public static void PrevDay()
        {
            CurrentShownDay = CurrentShownDay.AddDays(-1);
        }

        // --- Reset ---

        public static void Clear()
        {
            _currentShownMonth = null;
            _currentShownDay = null;
        }
    }
}
