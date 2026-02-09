using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;
using LocalCalendar.Data;
using LocalCalendar.Services;
using LocalCalendar.Prefabs;

namespace LocalCalendar.Controllers
{
    public class AgendaController : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] Header header;
        [SerializeField] SidePanelPopover sideMenuPopover;

        [Header("Layout")]
        [SerializeField] RectTransform contentRoot;
        [SerializeField] RectTransform timeGrid;
        [SerializeField] RectTransform itemsLayer;
        [SerializeField] RectTransform nowIndicator;

        [Header("Prefabs")]
        [SerializeField] RectTransform hourRowPrefab;
        [SerializeField] AgendaItemView agendaItemPrefab;

        private readonly List<GameObject> _spawned = new();
        private CalendarRepository _repo;
        private DateTime _visibleDate;

        const float HourHeight = 200f;

        void Start()
        {
            _repo = new CalendarRepository();
            _visibleDate = DateTime.Today;

            header.Configure(new HeaderConfig{ SideMenuPopover = sideMenuPopover });
            header.OnPrev += () => ChangeDay(-1);
            header.OnNext += () => ChangeDay(1);

            BuildHours();
            Refresh();
        }

        void Update()
        {
            UpdateNowIndicator();
        }

        void ChangeDay(int delta)
        {
            _visibleDate = _visibleDate.AddDays(delta);
            Refresh();
        }

        void Refresh()
        {
            ClearItems();

            header.title.text = _visibleDate.ToString("dddd, MMM d");
            header.currentDate = _visibleDate;

            var items = CalendarUtils.GetExpandedDayItems(_repo, _visibleDate);

            foreach (var (item, start) in items)
            {
                CreateItem(item, start);
            }

            UpdateNowIndicator();
        }

        void BuildHours()
        {
            for (int i = 0; i < 24; i++)
            {
                var row = Instantiate(hourRowPrefab, timeGrid);
                row.anchoredPosition = new Vector2(0, -i * HourHeight);

                var label = row.GetComponentInChildren<TMP_Text>();
                label.text = DateTime.Today.AddHours(i).ToString("h\ntt");//PlayerSettings.FormatTime(DateTime.Today.AddHours(i));
            }

            contentRoot.sizeDelta = new Vector2(
                contentRoot.sizeDelta.x,
                24 * HourHeight
            );
        }

        void CreateItem(CalendarItem item, DateTime startLocal)
        {
            var view = Instantiate(agendaItemPrefab, itemsLayer);
            _spawned.Add(view.gameObject);

            var start = startLocal;
            var end = CalendarUtils.GetOccurrenceEnd(item, start);

            if (end.Date > _visibleDate)
                end = _visibleDate.AddDays(1);

            float yStart = (float)start.TimeOfDay.TotalHours * HourHeight;
            float yEnd = (float)end.TimeOfDay.TotalHours * HourHeight;

            float height = Mathf.Max(30, yEnd - yStart);

            var rt = view.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.offsetMin = new Vector2(100, -yStart - height);
            rt.offsetMax = new Vector2(-20, -yStart);

            view.Bind(item, OnItemTapped);
        }

        void OnItemTapped(CalendarItem item)
        {
            EditItemContext.EditingItemId = item.Id;
            UnityEngine.SceneManagement.SceneManager.LoadScene("EditItemScene");
        }

        void UpdateNowIndicator()
        {
            if (_visibleDate.Date != DateTime.Today)
            {
                nowIndicator.gameObject.SetActive(false);
                return;
            }

            nowIndicator.gameObject.SetActive(true);

            float y = (float)DateTime.Now.TimeOfDay.TotalHours * HourHeight;

            var rt = nowIndicator;
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.offsetMin = new Vector2(0, -y - 2);
            rt.offsetMax = new Vector2(0, -y + 2);
        }

        void ClearItems()
        {
            foreach (var go in _spawned)
                Destroy(go);

            _spawned.Clear();
        }
    }
}
