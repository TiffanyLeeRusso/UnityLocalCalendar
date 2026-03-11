using System.Collections.Generic;
using UnityEngine;

namespace LocalCalendar.Services
{
    public static class SettingsService
    {
        private const string Use24HourKey   = "settings_use_24h";
        private const string WeekStartMonKey = "settings_week_start_mon";
        private const string CatsActiveKey = "cats_active";

        // --- 24 Hour Time ---

        public static bool GetUse24HourTime()
        {
            return PlayerPrefs.GetInt(Use24HourKey, 0) == 1;
        }

        public static void SetUse24HourTime(bool value)
        {
            PlayerPrefs.SetInt(Use24HourKey, value ? 1 : 0);
            PlayerPrefs.Save();
        }

        // --- Week Starts Monday ---

        public static bool GetWeekStartMonday()
        {
            return PlayerPrefs.GetInt(WeekStartMonKey, 0) == 1;
        }

        public static void SetWeekStartMonday(bool value)
        {
            PlayerPrefs.SetInt(WeekStartMonKey, value ? 1 : 0);
            PlayerPrefs.Save();
        }

        // --- Cats ---

        public static bool GetCatsActive()
        {
            return PlayerPrefs.GetInt(CatsActiveKey, 0) == 1;
        }

        public static void SetCatsActive(bool value)
        {
            PlayerPrefs.SetInt(CatsActiveKey, value ? 1 : 0);
            PlayerPrefs.Save();
        }

        // --- Convenience ---

        public static void ResetAll()
        {
            PlayerPrefs.DeleteKey(Use24HourKey);
            PlayerPrefs.DeleteKey(WeekStartMonKey);
            PlayerPrefs.Save();
        }
    }
}
