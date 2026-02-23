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
        private bool _isInitialized = false;

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

            _isInitialized = true;
        }

        private void OnRectTransformDimensionsChange()
        {
            if (_isInitialized)
            {
                // We can't change the grid in the same frame as a layout callback
                // so we trigger the update for the next frame.
                Relayout(); 
            }
        }
        
        public void Relayout()
        {
            if (!gameObject.activeInHierarchy) return;
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
            //yield return null; // wait for YearContainer GLG to assign MonthBlock its size
            //yield return null; // wait one more for child layout to propagate into dayGrid
            //Canvas.ForceUpdateCanvases();
            //LayoutRebuilder.ForceRebuildLayoutImmediate(transform.parent as RectTransform); // YearContainer
            //LayoutRebuilder.ForceRebuildLayoutImmediate(transform as RectTransform); // MonthBlock
            // Wait for the end of the frame so the VLG has 
            // officially finished setting our new width/height
            yield return new WaitForEndOfFrame();
            ApplySizing();
        }

        public void ApplySizing()
        {
            var gridLayout = dayGrid.GetComponent<GridLayoutGroup>();
            var monthBlockRT = GetComponent<RectTransform>();
    
            // Get the total height available to the whole MonthBlock
            float totalBlockHeight = monthBlockRT.rect.height;

            // Subtract the heights of the other elements in the Vertical Layout Group
            // We use LayoutUtility to get their "Preferred" heights (the space they actually take)
            float labelHeight = LayoutUtility.GetPreferredHeight(monthLabel.rectTransform);
            float headerHeight = LayoutUtility.GetPreferredHeight(weekdayHeader.GetComponent<RectTransform>());
    
            // Get the spacing from the Vertical Layout Group
            var vlg = GetComponent<VerticalLayoutGroup>();
            float vlgSpacing = vlg.spacing;
            float vlgPadding = vlg.padding.top + vlg.padding.bottom;

            // Space Left = Total - (Sum of other parts + spacing)
            // There are 2 gaps between 3 objects (Label, Header, Grid)
            float availableGridHeight = totalBlockHeight - vlgPadding - labelHeight - headerHeight - (vlgSpacing * 2);

            float availableGridWidth = monthBlockRT.rect.width - (vlg.padding.left + vlg.padding.right);

            // Calculate Cells
            const int columns = 7;
            const int rows = 6;

            float spacingX = gridLayout.spacing.x;
            float spacingY = gridLayout.spacing.y;
            float paddingX = gridLayout.padding.left + gridLayout.padding.right;
            float paddingY = gridLayout.padding.top + gridLayout.padding.bottom;

            float cellWidth = (availableGridWidth - paddingX - spacingX * (columns - 1)) / columns;
            float cellHeight = (availableGridHeight - paddingY - spacingY * (rows - 1)) / rows;

            if (cellWidth > 0 && cellHeight > 0)
            {
                gridLayout.cellSize = new Vector2(cellWidth, cellHeight);
                weekdayHeader.Build(cellWidth, GridTextFontSize);
            }

            // Final Rebuild
            LayoutRebuilder.ForceRebuildLayoutImmediate(dayGrid);
        }
    }
}
