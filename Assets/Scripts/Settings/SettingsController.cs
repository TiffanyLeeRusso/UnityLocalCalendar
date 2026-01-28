using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System;
using System.Collections;
using System.IO;
using LocalCalendar.App;
using LocalCalendar.Services;
using LocalCalendar.Data;
using LocalCalendar.Notifications;
using LocalCalendar.Permissions;

namespace LocalCalendar.Settings
{
    public class SettingsController : MonoBehaviour
    {
        [SerializeField] private SidePanelPopover sideMenuPopover;
        [SerializeField] private TMP_Text versionText;
        [SerializeField] private TMP_Text permissionsList;
        [SerializeField] private CanvasGroup permissionsGroup; // for the fade effect to show something happened
        [SerializeField] private CanvasGroup importExportStatusGroup; // for the fade effect to show something happened
        [SerializeField] private TMP_Text importExportStatusText;
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
            importExportStatusText.text = "";
            versionText.text = $"Version {Application.version}";

            LoadPermissions();
            LoadTheme();
            LoadTextMode();
        }

        private void OnEnable()
        {
            Database.OnImportFinished += HandleImportFinished;
        }

        private void OnDisable()
        {
            Database.OnImportFinished -= HandleImportFinished;
        }

        public void OpenSideMenu()
        {
            sideMenuPopover.gameObject.SetActive(true);
        }

        public void LoadPermissions()
        {
            StartCoroutine(LoadPermissionsRoutine());
        }

        IEnumerator LoadPermissionsRoutine()
        {
            yield return AppUtils.Fade(permissionsGroup, 1f, 0f, 0.25f);
            yield return new WaitForSeconds(0.5f);
            permissionsList.text = PermissionsUtils.GetPermissionsListAsString();
            yield return AppUtils.Fade(permissionsGroup, 0f, 1f, 0.25f);
        }

        public void RegrantPermissions()
        {
            PopupService.ShowPermissionsPopup();
        }

        public void ExportDB()
        {
            bool status = Database.ExportDB();
            SetImportExportStatus("Export", status);
        }

        public void ImportDB()
        {
            Database.ImportDB();
        }

        private void HandleImportFinished(ImportResult result)
        {
            switch (result)
            {
                case ImportResult.Success:
                    SetImportExportStatus("Import", true);
                    break;
                case ImportResult.InvalidFile:
                case ImportResult.Error:
                default:
                    SetImportExportStatus("Import", false);
                    break;
            }
        }

        private void SetImportExportStatus(string funcType, bool status)
        {
            string text = $"{funcType} " + (status ? "succeeded" : "failed") + ". See logs for details.";
            importExportStatusText.text = TMPUtils.ColorStatus(status, text, text);
        }

        public void CancelAllNotifications()
        {
            NotificationScheduler.CancelAll();
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
            NotificationScheduler.SendTestNotification();
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
