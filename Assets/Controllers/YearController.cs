using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using LocalCalendar.Prefabs;
using LocalCalendar.Services;

namespace LocalCalendar.Controllers
{
    public class YearController : MonoBehaviour
    {
        [SerializeField] private RectTransform rootCanvas;
        [SerializeField] private CanvasGroup mainCanvasGroup;
        [SerializeField] private Header header;
        [SerializeField] private RectTransform yearContainer;
        [SerializeField] private MonthBlock monthBlockPrefab;
        [SerializeField] private SidePanelPopover sideMenuPopover;

        private DateTime _currentYear; // Always Jan 1 of that year
        private GridLayoutGroup gridLayout;
        private LayoutElement gridLayoutElement;
        private bool _isRelayouting = false;

        void Awake()
        {
            gridLayout = yearContainer.GetComponent<GridLayoutGroup>();
            gridLayoutElement = yearContainer.GetComponent<LayoutElement>();

            // Ensure we have a LayoutElement for the shrinking-for-orientation-change code
            if (gridLayoutElement == null) 
                gridLayoutElement = yearContainer.gameObject.AddComponent<LayoutElement>();
        }

        void Start()
        {
            header.Configure(new HeaderConfig{ ShowToday = true,
                                               SideMenuPopover = sideMenuPopover });

            header.OnPrev += () => SetYear(_currentYear.AddYears(-1));
            header.OnNext += () => SetYear(_currentYear.AddYears(1));
            header.OnToday += () => SetYear(DateTime.Today);

            RebuildLayout();
        }

        void OnEnable()
        {
            LayoutWatcher.Instance.OnRelayout += HandleRelayout;
        }

        void OnDisable()
        {
            if (LayoutWatcher.Instance != null)
                LayoutWatcher.Instance.OnRelayout -= HandleRelayout;
        }

        // --- Layout handling ---

        private void HandleRelayout()
        {
            if (_isRelayouting) return;
            StopAllCoroutines();
            StartCoroutine(HandleRelayoutRoutine());
        }

        private IEnumerator HandleRelayoutRoutine()
        {
            _isRelayouting = true;

            // Hide everything during calculation or else the grid
            // collapse will show up as a flash of tiny grid.
            // We cannot use gameObject.SetActive(false) because
            // the layout engine stops calculating it entirely
            // and ResizeGrid math will return 0 because the RectTransform
            // doesn't exist while disabled.
            if (mainCanvasGroup != null) mainCanvasGroup.alpha = 0;

            // Collapse the grid so it stops pushing the parent VLG boundaries
            gridLayout.enabled = false;
            gridLayoutElement.preferredWidth = 0;
            gridLayoutElement.preferredHeight = 0;

            // Wait for the parent (MainContent) to shrink to the new screen size
            yield return null; // Wait 1 frame
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(rootCanvas);

            UpdateGridSizing();

            // Wait one more frame for the MonthBlock's DayGrid to realize
            // its parent MonthBlock has changed size.
            yield return null; 

            // Tell each month to resize its internal grid to the new space
            foreach (Transform child in yearContainer)
            {
                var block = child.GetComponent<MonthBlock>();
                if (block != null) block.ApplySizing(); 
            }
            
            // Show the UI
            if (mainCanvasGroup != null) mainCanvasGroup.alpha = 1;
            _isRelayouting = false;
        }

        private void UpdateGridSizing()
        {
            // Fit months to screen
            bool isLandscape = Screen.width > Screen.height;

            var grid = yearContainer.GetComponent<GridLayoutGroup>();

            int columns = isLandscape ? 4 : 3;
            int rows = isLandscape ? 3 : 4;

            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = columns;

            float totalWidth = yearContainer.rect.width;
            float totalHeight = yearContainer.rect.height;

            float spacingX = grid.spacing.x;
            float spacingY = grid.spacing.y;

            float paddingX = grid.padding.left + grid.padding.right;
            float paddingY = grid.padding.top + grid.padding.bottom;

            float cellWidth = (totalWidth - paddingX - (spacingX * (columns - 1))) / columns;
            float cellHeight = (totalHeight - paddingY - (spacingY * (rows - 1))) / rows;
            grid.cellSize = new Vector2(cellWidth, cellHeight);

            gridLayout.enabled = true;
            LayoutRebuilder.ForceRebuildLayoutImmediate(yearContainer);
        
            // Reset preferred size so it doesn't stay at 0
            gridLayoutElement.preferredWidth = -1;
            gridLayoutElement.preferredHeight = -1;

            LayoutRebuilder.ForceRebuildLayoutImmediate(yearContainer);
        }

        // --- Calendar building ---

        private void RebuildLayout(bool fullLayoutCalc = true)
        {
            Clear();
            BuildGridCells();

            if(fullLayoutCalc)
            {
                HandleRelayout();
            }
            else
            {
                UpdateGridSizing();
            }
        }

        private void BuildGridCells()
        {
            _currentYear = DateContext.CurrentShownYear;
            header.title.text = _currentYear.ToString("yyyy");

            for (int month = 1; month <= 12; month++)
            {
                var block = Instantiate(monthBlockPrefab, yearContainer);
                var monthDate = new DateTime(_currentYear.Year, month, 1);
                block.Initialize(monthDate, OnMonthClicked);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(yearContainer);
        }

        // --- Click Handlers ---

        private void SetYear(DateTime date)
        {
            // Normalize to January 1
            _currentYear = new DateTime(date.Year, 1, 1);
            DateContext.CurrentShownYear = _currentYear;

            RebuildLayout(false);
        }

        private void OnMonthClicked(DateTime monthDate)
        {
            DateContext.CurrentShownMonth = monthDate;
            SceneHistoryManager.Instance.LoadScene(AppScene.Calendar);
        }

        private void Clear()
        {
            foreach (Transform child in yearContainer)
                Destroy(child.gameObject);
        }
    }
}
