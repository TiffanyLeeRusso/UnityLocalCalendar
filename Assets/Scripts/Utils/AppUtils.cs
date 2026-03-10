using System;
using System.IO;
using System.Collections;
using UnityEngine;
using SQLite;
using LocalCalendar.Data;
using LocalCalendar.Services;

namespace LocalCalendar.Utils
{
    public static class AppUtils
    {
        public static string LIGHT_BG_TEXT_COLOR = "#111122";
        public static string DARK_BG_TEXT_COLOR = "#f0f0f8";
        public static string DARK_BG_LINK_COLOR = "#82B1FF";
        public static string DARK_BG_OK_TEXT_COLOR = "#4CAF50";
        public static string DARK_BG_BAD_TEXT_COLOR = "#F44336";
        
        // --- UI helpers ---

        public static IEnumerator Fade(CanvasGroup g, float from, float to, float duration)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                g.alpha = Mathf.Lerp(from, to, t / duration);
                yield return null;
            }

            g.alpha = to;
        }

        // --- DateTime Formatters ---

        // Formats a DateTime for display based on 12h / 24h user setting.
        public static string FormatTime(DateTime time, bool compactMode = false)
        {
            bool use24 = SettingsService.GetUse24HourTime();

            if (compactMode)
            {
                if (use24)
                    return time.ToString("HH");

                return time.ToString("h\ntt");
            }

            if (use24)
                return time.ToString("HH:mm");

            return time.ToString("h:mm tt");
        }

        // Formats a date
        public static string FormatDate(DateTime date)
        {
            return date.ToString("yyyy-MM-dd");
        }

        // Formats a date + time string based on user settings.
        // Year not included
        public static string FormatDateTime(DateTime time)
        {
            bool use24 = SettingsService.GetUse24HourTime();

            string timePart = use24
                ? time.ToString("HH:mm")
                : time.ToString("h:mm tt");

            return $"{time:ddd, MMM d} {timePart}";
        }

        // Formats a full date + time string based on user settings.
        // Year included
        public static string FormatFullDateTime(DateTime time)
        {
            bool use24 = SettingsService.GetUse24HourTime();

            string timePart = use24
                ? time.ToString("HH:mm")
                : time.ToString("h:mm tt");

            return $"{time:ddd, MMM d, yyyy} {timePart}";
        }

        public static Color FromHex(string hex)
	{
	    Color color;
	    if(ColorUtility.TryParseHtmlString(hex, out color)) { return color; }
	    else {
		Debug.Log($"Invalid color hex: {hex}");
		return Color.navyBlue;
	    }
	}
    }
}
