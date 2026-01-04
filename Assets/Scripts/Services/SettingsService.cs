using System.Collections.Generic;
using UnityEngine;
using LocalCalendar.Data;

namespace LocalCalendar.Services
{
    public static class SettingsService
    {
        private const string ThemeKey = "theme";
        private const string TextModeKey = "text_mode";
        private const string PresetsKey = "reminder_presets";

        public static string GetTheme()
        {
            return PlayerPrefs.GetString(ThemeKey, "light");
        }

        public static void SetTheme(string theme)
        {
            PlayerPrefs.SetString(ThemeKey, theme);
            PlayerPrefs.Save();
        }

        public static string GetTextMode()
        {
            return PlayerPrefs.GetString(TextModeKey, "normal");
        }

        public static void SetTextMode(string mode)
        {
            PlayerPrefs.SetString(TextModeKey, mode);
            PlayerPrefs.Save();
        }

        public static List<ReminderPreset> GetPresets()
        {
            if (!PlayerPrefs.HasKey(PresetsKey))
                return GetDefaultPresets();

            string json = PlayerPrefs.GetString(PresetsKey);
            return JsonUtility.FromJson<PresetWrapper>(json).Items;
        }

        public static void SavePresets(List<ReminderPreset> presets)
        {
            var wrapper = new PresetWrapper { Items = presets };
            PlayerPrefs.SetString(PresetsKey, JsonUtility.ToJson(wrapper));
            PlayerPrefs.Save();
        }

        private static List<ReminderPreset> GetDefaultPresets()
        {
            return new List<ReminderPreset>
            {
                new ReminderPreset { Name = "Morning", Time = System.TimeSpan.FromHours(8) },
                new ReminderPreset { Name = "Evening", Time = System.TimeSpan.FromHours(18) }
            };
        }

        [System.Serializable]
        private class PresetWrapper
        {
            public List<ReminderPreset> Items;
        }
    }
}
