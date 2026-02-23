using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using LocalCalendar.Prefabs;
using LocalCalendar.Data;

namespace LocalCalendar.Services
{
    public class SidePanelPopover : MonoBehaviour
    {
        [SerializeField] private TMP_InputField searchInput;
        [SerializeField] private RectTransform searchResults;
        [SerializeField] private DayEventRow resultPrefab;
        [SerializeField] private TMP_Text noResultsLabel;

        const int MAX_SEARCH_RESULTS = 20;
        private CalendarRepository _repo;
        private readonly List<DayEventRow> _resultItems = new();
        private Coroutine _searchRoutine;

        // --- Init ---

        void Start()
        {
            _repo = new CalendarRepository();
            searchInput.onValueChanged.AddListener(OnSearchChanged);
        }

        // --- Buttons ---

        public void OpenCalendar()
        {
            SceneHistoryManager.Instance.LoadScene(AppScene.Calendar);
        }

        public void OpenAgenda()
        {
            SceneHistoryManager.Instance.LoadScene(AppScene.Agenda);
        }

        public void OpenYear()
        {
            SceneHistoryManager.Instance.LoadScene(AppScene.Year);
        }

        public void OpenSchedule()
        {
            SceneHistoryManager.Instance.LoadScene(AppScene.Schedule);
        }

        public void OpenSettings()
        {
            SceneHistoryManager.Instance.LoadScene(AppScene.Settings);
        }

        public void Close()
        {
            gameObject.SetActive(false);
        }

        // --- Search ---

        private void OnSearchChanged(string text)
        {
            if (_searchRoutine != null)
                StopCoroutine(_searchRoutine);

            _searchRoutine = StartCoroutine(SearchDelayed(text));
        }

        private IEnumerator SearchDelayed(string text)
        {
            yield return new WaitForSeconds(0.2f);

            ClearResults();

            if (string.IsNullOrWhiteSpace(text))
                yield break;

            var results = _repo.Search(text).Take(MAX_SEARCH_RESULTS).ToList();
            if (results.Count == 0)
            {
                ShowNoResults();
                yield break;
            }

            foreach (var item in results)
                AddResult(item);
        }

        /* Debounce the input instead.
        private void OnSearchChanged(string text)
        {
            ClearResults();

            if (string.IsNullOrWhiteSpace(text))
                return;

            var results = _repo.Search(text).Take(MAX_SEARCH_RESULTS).ToList();

            foreach (var item in results)
                AddResult(item);
        }
        */

        private void AddResult(CalendarItem item)
        {
            var row = Instantiate(resultPrefab, searchResults);

            DateTime occurrenceStart = item.StartUtc;

            row.Initialize(
                item,
                occurrenceStart,
                args => OpenItem(args.item),
                occurrenceStart,
                DayEventRow.ViewMode.Compact
            );

            _resultItems.Add(row);
        }

        private void ClearResults()
        {
            foreach (var r in _resultItems)
                Destroy(r.gameObject);

            _resultItems.Clear();

            if (noResultsLabel != null)
                noResultsLabel.gameObject.SetActive(false);
        }

        private void ShowNoResults()
        {
            noResultsLabel.gameObject.SetActive(true);
        }

        private void OpenItem(CalendarItem item)
        {
            EditItemContext.EditingItemId = item.Id;
            EditItemContext.Mode = EditItemMode.Preview;
            SceneHistoryManager.Instance.LoadScene(AppScene.EditItem);
        }
    }
}
