using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Unity.Notifications.Android;
using System;
using LocalCalendar.Services;
using LocalCalendar.Data;
using LocalCalendar.Notifications;
using LocalCalendar.Permissions;

namespace LocalCalendar.Settings
{
    public class SettingsController : MonoBehaviour
    {
        [SerializeField] private TMP_Text permissionsList;
        [SerializeField] private Toggle darkToggle;
        [SerializeField] private Toggle bigTextToggle;

        [SerializeField] private TMP_InputField dbPathText;
        [SerializeField] private GameObject debugPopup;
        [SerializeField] private RectTransform debugScrollViewRect;
        [SerializeField] private TextMeshProUGUI debugText;
        [SerializeField] private GameObject debugDiagnosticsClearBtn;
        [SerializeField] private GameObject debugCrashClearBtn;
        [SerializeField] private Button debugRefreshBtn;

        void Start()
        {
            dbPathText.text = Application.persistentDataPath;

            LoadPermissions();
            LoadTheme();
            LoadTextMode();
        }

        public void LoadPermissions()
        {
            permissionsList.text = PermissionsUtils.GetPermissionsListAsString();
        }

        public void OpenCalendar()
        {
            SceneManager.LoadScene("CalendarScene");
        }

        public void RegrantPermissions()
        {
            PopupService.ShowPermissionsPopup();
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

            debugDiagnosticsClearBtn.SetActive(false);
            debugCrashClearBtn.SetActive(false);
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
            LoggingService.ClearDebug();
            DumpDiagnostics();
        }

        public void ClearCrash()
        {
            LoggingService.ClearCrashData();
            DumpDiagnostics();
        }

        public void OpenDiagnosticsPopup()
        {
            DumpDiagnostics();

            debugDiagnosticsClearBtn.SetActive(true);
            debugCrashClearBtn.SetActive(true);

            debugRefreshBtn.onClick.RemoveAllListeners();
            debugRefreshBtn.onClick.AddListener(DumpDiagnostics);
            debugPopup.SetActive(true);
        }
    }
}
