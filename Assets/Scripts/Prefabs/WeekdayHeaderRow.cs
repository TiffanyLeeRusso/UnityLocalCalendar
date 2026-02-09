using System;
using UnityEngine;
using UnityEngine.UI;

namespace LocalCalendar.Prefabs
{
    public class WeekdayHeaderRow : MonoBehaviour
    {
        [SerializeField] Transform container;
        [SerializeField] WeekdayHeaderCell cellPrefab;

        public void Build(float cellWidth)
        {
            foreach (Transform c in container)
                Destroy(c.gameObject);

            var culture = System.Globalization.CultureInfo.CurrentCulture;
            var names = culture.DateTimeFormat.AbbreviatedDayNames;

            // Start Monday instead of Sunday if you want:
            //int start = (int)DayOfWeek.Monday;
            int start = (int)DayOfWeek.Sunday;

            for (int i = 0; i < 7; i++)
            {
                int idx = (start + i) % 7;
                var cell = Instantiate(cellPrefab, container);

                cell.Set(names[idx]);

                var le = cell.GetComponent<LayoutElement>();
                if (le != null)
                    le.preferredWidth = cellWidth;
            }
        }
    }
}
