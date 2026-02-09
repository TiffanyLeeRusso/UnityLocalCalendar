using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using LocalCalendar.Services;

namespace LocalCalendar.Prefabs
{
    public class Header : MonoBehaviour, IBackHandler
    {
        // Left button can be side menu or a close/back button
        [SerializeField] public Button sidePanelButton;
        [SerializeField] public Button backButton;
        [SerializeField] public Button addButton;
        [SerializeField] public Button prevButton;
        [SerializeField] public Button nextButton;
        [SerializeField] public Button todayButton;
        [SerializeField] public TMP_Text sceneTitle;
        [SerializeField] public TMP_Text title; // Dynamic

        public SidePanelPopover sideMenuPopover;
        public DateTime? currentDate;
        public event Action OnBack;
        public event Action OnPrev;
        public event Action OnNext;
        public event Action OnAdd;
        public event Action OnToday;

        void Awake()
        {
            backButton.onClick.AddListener(() => OnBack?.Invoke());
            addButton.onClick.AddListener(() => OnAdd?.Invoke());
            prevButton.onClick.AddListener(() => OnPrev?.Invoke());
            nextButton.onClick.AddListener(() => OnNext?.Invoke());
            todayButton.onClick.AddListener(() => OnToday?.Invoke());
        }

        void OnDisable()
        {
            if (SceneHistoryManager.Exists)
                SceneHistoryManager.Instance.UnregisterHandler(this);
        }

        public void Configure(HeaderConfig config)
        {
            title.text = config.Title;
            sceneTitle.text = config.SceneTitle;
            currentDate = config.CurrentDate;
            sideMenuPopover = config.SideMenuPopover;

            sidePanelButton.gameObject.SetActive(config.ShowSidePanel);
            backButton.gameObject.SetActive(config.ShowBack);
            addButton.gameObject.SetActive(config.ShowAdd);
            prevButton.gameObject.SetActive(config.ShowPrev);
            nextButton.gameObject.SetActive(config.ShowNext);
            todayButton.gameObject.SetActive(config.ShowToday);
        }

        public bool OnBackButtonPressed()
        {
            if (sideMenuPopover.gameObject.activeSelf)
            {
                sideMenuPopover.gameObject.SetActive(false);
                return true;
            }
            return false; // Let the manager switch scenes
        }

        public void AddItem()
        {
            EditItemContext.SelectedDate = currentDate ?? DateTime.Today;
            SceneHistoryManager.Instance.LoadScene(AppScene.EditItem);
        }

        public void OpenSideMenu()
        {
            sideMenuPopover.gameObject.SetActive(true);
        }
    }
}
