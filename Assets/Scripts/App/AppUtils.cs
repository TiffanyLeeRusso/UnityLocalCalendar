using System;
using System.IO;
using System.Collections;
using UnityEngine;
using SQLite;
using LocalCalendar.Data;
using LocalCalendar.Services;

namespace LocalCalendar.App
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
    }
}
