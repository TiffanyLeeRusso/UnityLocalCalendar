using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;
using LocalCalendar.Data;
using LocalCalendar.Services;
using LocalCalendar.Utils;
using LocalCalendar.Prefabs;

namespace LocalCalendar.Controllers
{
    class LayoutItem
    {
        public CalendarItem item;
        public DateTime start;
        public DateTime end;
        public AgendaItemView view;

        public int column;
        public int columnCount;
    }

    public class AgendaController : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] Header header;
        [SerializeField] SidePanelPopover sideMenuPopover;
        [SerializeField] GameObject nowCat;

        [Header("Layout")]
        [SerializeField] RectTransform staticEventsContainer; // The row itself
        [SerializeField] RectTransform staticEventsContent;   // The VLG child that holds items
        [SerializeField] RectTransform contentRoot;
        [SerializeField] RectTransform timeGrid;
        [SerializeField] RectTransform itemsLayer;
        [SerializeField] RectTransform nowIndicator;
        [SerializeField] TMP_Text nowTimeLabel;
        [SerializeField] RectTransform nowArrow;
        [SerializeField] RectTransform nowLine;

        [Header("Prefabs")]
        [SerializeField] RectTransform hourRowPrefab;
        [SerializeField] AgendaItemView agendaItemPrefab;

        private readonly List<GameObject> _spawned = new();
        private CalendarRepository _repo;
        private DateTime _visibleDate;
        private float _nextUpdate;
        private List<LayoutItem> _staticItems;
        private List<LayoutItem> _layoutItems;

        const float HourHeight = 250f;
        const float MinItemHeight = 90f;

        void OnEnable()
        {
            LayoutWatcher.Instance.OnRelayout += HandleRelayout;
        }

        void OnDisable()
        {
            if (LayoutWatcher.Instance != null)
                LayoutWatcher.Instance.OnRelayout -= HandleRelayout;
        }

        void Start()
        {
            _repo = new CalendarRepository();
            _visibleDate = DateContext.CurrentShownDay;

            nowCat.SetActive(SettingsService.GetCatsActive());

            header.Configure(new HeaderConfig{ SideMenuPopover = sideMenuPopover,
                                               ShowToday = true });
            header.OnPrev += PrevDay;
            header.OnNext += NextDay;
            header.OnToday += Today;

            BuildHours();
            Refresh();
        }

        void Update()
        {
            if (Time.time >= _nextUpdate)
            {
                _nextUpdate = Time.time + 30f;
                UpdateNowIndicator();
            }
        }

        void HandleRelayout()
        {
            StopAllCoroutines();
            StartCoroutine(RelayoutRoutine());
        }

        void PrevDay()
        {
            DateContext.PrevDay();
            _visibleDate = DateContext.CurrentShownDay;
            Refresh();
        }

        void NextDay()
        {
            DateContext.NextDay();
            _visibleDate = DateContext.CurrentShownDay;
            Refresh();
        }

        void Today()
        {
            DateContext.Today();
            _visibleDate = DateContext.CurrentShownDay;
            Refresh();
        }

        private void BuildHours()
        {
            for (int i = 0; i < 24; i++)
            {
                var row = Instantiate(hourRowPrefab, timeGrid);
                row.anchoredPosition = new Vector2(0, -i * HourHeight);

                var label = row.GetComponentInChildren<TMP_Text>();
                label.text = AppUtils.FormatTime(
                    DateTime.Today.AddHours(i),
                    compactMode: true
                );
            }

            contentRoot.sizeDelta = new Vector2(
                contentRoot.sizeDelta.x,
                24 * HourHeight
            );
        }

        private void OnItemTapped(CalendarItem item)
        {
            EditItemContext.EditingItemId = item.Id;
            EditItemContext.Mode = EditItemMode.Preview;
            SceneHistoryManager.Instance.LoadScene(AppScene.EditItem);
        }

        void UpdateNowIndicator()
        {
            if (_visibleDate.Date != DateTime.Today)
            {
                nowIndicator.gameObject.SetActive(false);
                return;
            }

            nowIndicator.gameObject.SetActive(true);

            var now = DateTime.Now;
            nowTimeLabel.text = AppUtils.FormatTime(now);

            float y = (float)now.TimeOfDay.TotalHours * HourHeight;

            var rt = nowIndicator;
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 0.5f);

            float height = rt.sizeDelta.y;

            rt.offsetMin = new Vector2(0, -y - height * 0.5f);
            rt.offsetMax = new Vector2(0, -y + height * 0.5f);
        }

        private void ClearItems()
        {
            foreach (var go in _spawned)
                Destroy(go);

            _spawned.Clear();
        }

        // --- Item-layout stuff ---

        IEnumerator RelayoutRoutine()
        {
            yield return new WaitForEndOfFrame();
            staticEventsContainer.gameObject.SetActive(false);
            Canvas.ForceUpdateCanvases();
            yield return null;

            LayoutStaticItems(_staticItems);
            LayoutItems(_layoutItems);
            UpdateNowIndicator();
        }

        private void Refresh()
        {
            StopAllCoroutines();
            StartCoroutine(RefreshRoutine());
        }

        private IEnumerator RefreshRoutine()
        {
            ClearItems();

            header.title.text = _visibleDate.ToString("ddd, MMM d, yyyy");
            header.currentDate = _visibleDate;

            // Hide layer to avoid flash
            //itemsLayer.gameObject.SetActive(false);

            // Wait for layout system
            yield return null;
            Canvas.ForceUpdateCanvases();
            yield return null;

            var items = CalendarUtils.GetExpandedDayItems(_repo, _visibleDate);

            _layoutItems = new List<LayoutItem>();
            _staticItems = new List<LayoutItem>();

            foreach (var (item, start) in items)
            {
                // Determine if it is a static event
                bool isMultiDay = (item.EndUtc - item.StartUtc).TotalDays >= 1.0;
                bool isStatic = item.AllDay || isMultiDay;

                // Instantiate in the correct parent
                RectTransform parent = isStatic ? staticEventsContent : itemsLayer;
                var view = Instantiate(agendaItemPrefab, parent);
                var visuals = view.GetComponent<CalendarItemVisuals>();
                visuals.ApplyStyle(item.Color);
                _spawned.Add(view.gameObject);

                var end = CalendarUtils.GetOccurrenceEnd(item, start);
                if (end.Date > _visibleDate)
                    end = _visibleDate.AddDays(1);

                view.Bind(item, OnItemTapped);

                var layoutItem = new LayoutItem { item = item,
                                                  start = start,
                                                  end = end,
                                                  view = view };
                if (isStatic)
                    _staticItems.Add(layoutItem);
                else
                    _layoutItems.Add(layoutItem);
            }

            LayoutStaticItems(_staticItems);
            LayoutItems(_layoutItems);
            UpdateNowIndicator();

            // Show after positioned
            //itemsLayer.gameObject.SetActive(true);
        }

        private void LayoutStaticItems(List<LayoutItem> staticItems)
        {
            if (staticItems == null || staticItems.Count == 0)
            {
                staticEventsContainer.gameObject.SetActive(false);
                return;
            }

            staticEventsContainer.gameObject.SetActive(true);

            float spacing = staticEventsContent.GetComponent<VerticalLayoutGroup>().spacing;
            float totalHeight = (staticItems.Count * MinItemHeight) + (Mathf.Max(0, staticItems.Count - 1) * spacing);

            var containerLE = staticEventsContainer.GetComponent<LayoutElement>();
            if (containerLE != null)
            {
                containerLE.preferredHeight = totalHeight;
            }

            foreach (var item in staticItems)
            {
                var rt = item.view.GetComponent<RectTransform>();
                // Ensure anchors are set to stretch horizontally
                rt.anchorMin = new Vector2(0, 0);
                rt.anchorMax = new Vector2(1, 1);
        
                var le = item.view.GetComponent<LayoutElement>() ?? item.view.gameObject.AddComponent<LayoutElement>();
                le.preferredHeight = MinItemHeight;
                le.flexibleWidth = 1;
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(staticEventsContent);
            LayoutRebuilder.ForceRebuildLayoutImmediate(staticEventsContainer);
    
            var mainVLG = staticEventsContainer.parent.GetComponent<RectTransform>();
            LayoutRebuilder.ForceRebuildLayoutImmediate(mainVLG);
        }

        private void LayoutItems(List<LayoutItem> items)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(itemsLayer);

            var ungrouped = new List<LayoutItem>(items);

            while (ungrouped.Count > 0)
            {
                var seed = ungrouped[0];
                ungrouped.RemoveAt(0);

                var group = new List<LayoutItem> { seed };

                bool added;
                do
                {
                    added = false;

                    for (int i = ungrouped.Count - 1; i >= 0; i--)
                    {
                        var test = ungrouped[i];

                        foreach (var g in group)
                        {
                            if (Overlaps(test, g))
                            {
                                group.Add(test);
                                ungrouped.RemoveAt(i);
                                added = true;
                                break;
                            }
                        }
                    }

                } while (added);

                LayoutGroup(group);
            }
        }

        void ApplyLayout(LayoutItem item, int col, int colCount)
        {
            // Figure out the height
            float yStart = (float)item.start.TimeOfDay.TotalHours * HourHeight;
            float yEnd   = (float)item.end.TimeOfDay.TotalHours   * HourHeight;

            float rawHeight = yEnd - yStart;
            float height = Mathf.Max(MinItemHeight, rawHeight);

            var rt = item.view.GetComponent<RectTransform>();

            // --- lock transform mode ---
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot     = new Vector2(0, 1);

            float leftPadding  = 120f;
            float rightPadding = 20f;
            float spacing      = 4f;

            float layerWidth = itemsLayer.rect.width;

            // safety guard
            if (layerWidth <= 0)
                return;

            float totalWidth = layerWidth - leftPadding - rightPadding;
            totalWidth = Mathf.Max(10, totalWidth);

            colCount = Mathf.Max(1, colCount);

            float colWidth = (totalWidth - spacing * (colCount - 1)) / colCount;
            colWidth = Mathf.Max(20, colWidth);

            float x = leftPadding + col * (colWidth + spacing);

            rt.anchoredPosition = new Vector2(x, -yStart);
            rt.sizeDelta        = new Vector2(colWidth, height);
        }

        void LayoutGroup(List<LayoutItem> group)
        {
            // Sort by start time for stable layout
            group.Sort((a, b) => a.start.CompareTo(b.start));

            foreach (var item in group)
            {
                // Build local overlap set for THIS item
                var local = new List<LayoutItem>();

                foreach (var other in group)
                {
                    if (Overlaps(item, other))
                        local.Add(other);
                }

                // Sort local overlaps by start time
                local.Sort((a, b) => a.start.CompareTo(b.start));

                // Pack local overlaps into columns
                var columns = new List<List<LayoutItem>>();

                foreach (var l in local)
                {
                    bool placed = false;

                    for (int c = 0; c < columns.Count; c++)
                    {
                        var last = columns[c][columns[c].Count - 1];

                        if (last.end <= l.start)
                        {
                            columns[c].Add(l);
                            placed = true;
                            break;
                        }
                    }

                    if (!placed)
                    {
                        columns.Add(new List<LayoutItem> { l });
                    }
                }

                int colCount = columns.Count;

                // Find THIS item's local column
                int myCol = 0;

                for (int c = 0; c < columns.Count; c++)
                {
                    if (columns[c].Contains(item))
                    {
                        myCol = c;
                        break;
                    }
                }

                // Apply layout using local column info
                ApplyLayout(item, myCol, colCount);
            }
        }

        private bool Overlaps(LayoutItem a, LayoutItem b)
        {
            return a.start < b.end && b.start < a.end;
        }
    }
}
