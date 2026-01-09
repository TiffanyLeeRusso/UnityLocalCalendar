using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Unity.Notifications.Android;
using System;
using LocalCalendar.Services;
using LocalCalendar.Data;
using LocalCalendar.Notifications;

namespace LocalCalendar.Settings
{
    public class SettingsController : MonoBehaviour
    {
        [Header("Storage")]
        [SerializeField] private TMP_InputField dbPathText;

        [Header("Theme")]
        [SerializeField] private Toggle darkToggle;

        [Header("BigTextMode")]
        [SerializeField] private Toggle bigTextToggle;

        [Header("Debug")]
        [SerializeField] private GameObject debugPopup;
        [SerializeField] private RectTransform debugScrollViewRect;
        [SerializeField] private TextMeshProUGUI debugText;
        //[SerializeField] private TMP_InputField debugText;
        [SerializeField] private GameObject debugClearBtn;
        [SerializeField] private Button debugRefreshBtn;

        void Start()
        {
            dbPathText.text = Application.persistentDataPath;
            LoadTheme();
            LoadTextMode();
        }

        public void OpenCalendar()
        {
            SceneManager.LoadScene("CalendarScene");
        }

        public void CopyDbPath()
        {
            GUIUtility.systemCopyBuffer = Application.persistentDataPath;
        }

        void LoadTheme()
        {
            string theme = SettingsService.GetTheme();
            darkToggle.isOn = theme == "dark";
        }

        public void SetTheme(bool on)
        {
            if (on) SettingsService.SetTheme("dark");
            else SettingsService.SetTheme("light");
        }

        void LoadTextMode()
        {
            string mode = SettingsService.GetTextMode();
            bigTextToggle.isOn = mode == "big";
        }

        public void SetTextMode(bool on)
        {
            if (on) SettingsService.SetTextMode("big");
            else SettingsService.SetTextMode("normal");
        }

        // ---------- DEBUG ----------

        public void SendTestNotification()
        {
            var notification = new AndroidNotification
            {
                Title = "Test Notification",
                Text = "If you see this, notifications are working.",
                FireTime = DateTime.Now.AddSeconds(10)
            };

            AndroidNotificationCenter.SendNotification(
                notification,
                NotificationInitializer.Channel
            );
        }

        public void CloseDebug()
        {
            debugPopup.SetActive(false);
        }

        private void UpdateDebugText(string txt)
        {
            debugText.text = txt;
            debugText.ForceMeshUpdate();
            LayoutRebuilder.ForceRebuildLayoutImmediate(debugText.rectTransform);
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(debugScrollViewRect);
        }

        public void CopyDebugToClipboard()
        {
            if (string.IsNullOrEmpty(debugText.text))
                return;

            GUIUtility.systemCopyBuffer = debugText.text;

            LoggingService.Info(LogCategory.UI,
                                "Diagnostics log copied to clipboard");
        }


        // Debug Popup: DB Dump

        private void DumpDatabase()
        {
            var repo = new CalendarRepository();
            UpdateDebugText(repo.AllDBToString(repo.GetAll()));
        }

        public void OpenDatabasePopup()
        {
            DumpDatabase();

            debugClearBtn.SetActive(false);
            debugRefreshBtn.onClick.RemoveAllListeners();
            debugRefreshBtn.onClick.AddListener(DumpDatabase);
            debugPopup.SetActive(true);
        }

        // Debug Popup: Log Dump

        private void DumpDiagnostics()
        {
            UpdateDebugText(LoggingService.DumpAllToString());
        }

        public void ClearDiagnostics()
        {
            LoggingService.Clear();
            DumpDiagnostics();
        }

        public void OpenDiagnosticsPopup()
        {
            DumpDiagnostics();

            debugClearBtn.SetActive(true);
            debugRefreshBtn.onClick.RemoveAllListeners();
            debugRefreshBtn.onClick.AddListener(DumpDiagnostics);
            debugPopup.SetActive(true);
        }
    }
}
