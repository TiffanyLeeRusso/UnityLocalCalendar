using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using LocalCalendar.Services;

namespace LocalCalendar.Prefabs
{
    public class WeekdayHeaderRow : MonoBehaviour
    {
        [SerializeField] Transform container;
        [SerializeField] WeekdayHeaderCell cellPrefab;

        public void Build(float cellWidth, float fontSize = 50f)
        {
            foreach (Transform c in container)
                Destroy(c.gameObject);

            var culture = System.Globalization.CultureInfo.CurrentCulture;
            var names = culture.DateTimeFormat.AbbreviatedDayNames;

            int start = SettingsService.GetWeekStartMonday()
                ? (int)DayOfWeek.Monday
                : (int)DayOfWeek.Sunday;

            for (int i = 0; i < 7; i++)
            {
                int idx = (start + i) % 7;
                var cell = Instantiate(cellPrefab, container);

                cell.Set(names[idx]);
                if (fontSize > 0) cell.Layout(fontSize);

                var le = cell.GetComponent<LayoutElement>();
                if (le != null)
                    le.preferredWidth = cellWidth;
            }
        }
    }
}
