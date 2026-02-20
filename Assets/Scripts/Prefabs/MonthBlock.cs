using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using LocalCalendar.Data;

namespace LocalCalendar.Prefabs
{
    public class MonthBlock : MonoBehaviour
    {
        [SerializeField] private TMP_Text monthLabel;
        [SerializeField] private WeekdayHeaderRow weekdayHeader;
        [SerializeField] private RectTransform dayGrid;
        [SerializeField] private DayCell dayCellPrefab;

        private DateTime _monthDate;
        private Action<DateTime> _onMonthClicked;
        // Font size to use for day numbers in year view
        private const float GridTextFontSize = 15f;

        public void Initialize(DateTime monthDate, Action<DateTime> onMonthClicked)
        {
            _monthDate = new DateTime(monthDate.Year, monthDate.Month, 1);
            _onMonthClicked = onMonthClicked;
            monthLabel.text = _monthDate.ToString("MMMM");

            BuildGrid();

            // Entire month clickable
            var button = GetComponent<Button>();
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => _onMonthClicked?.Invoke(_monthDate));
            }

            // Defer sizing until layout has settled
            StartCoroutine(ApplySizingAfterLayout());
        }

        private void BuildGrid()
        {
            foreach (Transform child in dayGrid)
                Destroy(child.gameObject);

            DateTime firstDay = _monthDate;
            int startOffset = (int)firstDay.DayOfWeek;

            for (int i = 0; i < 42; i++)
            {
                DateTime cellDate = firstDay.AddDays(i - startOffset);

                bool isCurrentMonth = cellDate.Month == _monthDate.Month;
                bool isToday = cellDate.Date == DateTime.Today.Date
                    && _monthDate.Year == DateTime.Today.Year;;

                var cell = Instantiate(dayCellPrefab, dayGrid);
                cell.Initialize(
                    cellDate,
                    isToday && _monthDate.Year == DateTime.Today.Year,
                    isCurrentMonth,
                    new List<(CalendarItem item, DateTime occurrenceStart)>(), // no events
                    null, // day click disabled in year view
                    null  // item click disabled
                );

                cell.Layout(fontSize: GridTextFontSize,
                            showEvents: false,
                            allowCellClick: false);
            }
        }

        private IEnumerator ApplySizingAfterLayout()
        {
            yield return null; // wait for YearContainer GLG to assign MonthBlock its size
            yield return null; // wait one more for child layout to propagate into dayGrid
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(transform.parent as RectTransform); // YearContainer
            ApplySizing();
        }

        private void ApplySizing()
        {
            var gridLayout = dayGrid.GetComponent<GridLayoutGroup>();
            if (gridLayout == null) return;

            const int columns = 7;
            const int rows = 6;

            float totalWidth  = dayGrid.rect.width;
            float totalHeight = dayGrid.rect.height;

            float spacingX = gridLayout.spacing.x;
            float spacingY = gridLayout.spacing.y;
            float paddingX = gridLayout.padding.left + gridLayout.padding.right;
            float paddingY = gridLayout.padding.top  + gridLayout.padding.bottom;

            float cellWidth  = (totalWidth  - paddingX - spacingX * (columns - 1)) / columns;
            float cellHeight = (totalHeight - paddingY - spacingY * (rows    - 1)) / rows;

            gridLayout.cellSize = new Vector2(cellWidth, cellHeight);
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = columns;

            // Now we have a real cellWidth, build the weekday header
            weekdayHeader.Build(cellWidth, GridTextFontSize);

            LayoutRebuilder.ForceRebuildLayoutImmediate(dayGrid);
        }
    }
}
