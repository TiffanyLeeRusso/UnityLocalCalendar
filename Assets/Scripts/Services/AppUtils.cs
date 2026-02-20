using System;
using System.IO;
using System.Collections;
using UnityEngine;
using SQLite;
using LocalCalendar.Data;

namespace LocalCalendar.Services
{
    public static class AppUtils
    {
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

        // Formats a full date + time string based on user settings.
        public static string FormatDateTime(DateTime time)
        {
            bool use24 = SettingsService.GetUse24HourTime();

            string timePart = use24
                ? time.ToString("HH:mm")
                : time.ToString("h:mm tt");

            return $"{time:ddd, MMM d} {timePart}";
        }
    }
}
