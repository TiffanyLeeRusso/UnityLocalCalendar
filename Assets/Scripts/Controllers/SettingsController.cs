using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System;
using System.Collections;
using System.IO;
using LocalCalendar.Services;
using LocalCalendar.Data;
using LocalCalendar.Utils;
using LocalCalendar.Notifications;
using LocalCalendar.Permissions;

namespace LocalCalendar.Controllers
{
    public class SettingsController : MonoBehaviour, IBackHandler
    {
        [SerializeField] private SidePanelPopover sideMenuPopover;
        [SerializeField] private TMP_Text versionText;
        [SerializeField] private TMP_Text permissionsList;
        [SerializeField] private CanvasGroup permissionsGroup; // for the fade effect to show something happened
        [SerializeField] private Toggle twentyFourHrToggle;
        [SerializeField] private Toggle monWeekStartToggle;
        [SerializeField] private Toggle catToggle;
        [SerializeField] private CanvasGroup importExportStatusGroup; // for the fade effect to show something happened
        [SerializeField] private TMP_Text importExportStatusText;

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

            SceneHistoryManager.Instance.RegisterHandler(this);

            LoadPermissions();
            LoadViewSettings();
        }

        private void OnEnable()
        {
            Database.OnImportFinished += HandleImportFinished;
        }

        private void OnDisable()
        {
            if (SceneHistoryManager.Exists)
                SceneHistoryManager.Instance.UnregisterHandler(this);

            Database.OnImportFinished -= HandleImportFinished;
        }

        public bool OnBackButtonPressed()
        {
            if (debugPopup.activeSelf)
            {
                debugPopup.SetActive(false);
                return true;
            }
            else if (sideMenuPopover.gameObject.activeSelf)
            {
                sideMenuPopover.gameObject.SetActive(false);
                return true;
            }
            return false; // Let the manager switch scenes
        }

        public void OpenSideMenu()
        {
            sideMenuPopover.gameObject.SetActive(true);
        }
        
        // --- Permissions ---

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

        // --- View Settings ---

        private void LoadViewSettings()
        {
            // --- Load values ---
            bool use24h = SettingsService.GetUse24HourTime();
            bool weekMon = SettingsService.GetWeekStartMonday();
            bool catsActive = SettingsService.GetCatsActive();

            // Prevent callbacks while assigning
            twentyFourHrToggle.SetIsOnWithoutNotify(use24h);
            monWeekStartToggle.SetIsOnWithoutNotify(weekMon);
            catToggle.SetIsOnWithoutNotify(catsActive);

            // Clear old listeners
            twentyFourHrToggle.onValueChanged.RemoveAllListeners();
            monWeekStartToggle.onValueChanged.RemoveAllListeners();
            catToggle.onValueChanged.RemoveAllListeners();

            // --- Bind ---
            twentyFourHrToggle.onValueChanged.AddListener(On24HourToggleChanged);
            monWeekStartToggle.onValueChanged.AddListener(OnWeekStartToggleChanged);
            catToggle.onValueChanged.AddListener(OnCatToggleChanged);
        }

        private void On24HourToggleChanged(bool value)
        {
            SettingsService.SetUse24HourTime(value);
        }

        private void OnWeekStartToggleChanged(bool value)
        {
            SettingsService.SetWeekStartMonday(value);
        }

        private void OnCatToggleChanged(bool value)
        {
            SettingsService.SetCatsActive(value);
        }

        // --- Import/Export ---

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

        
        // --- Tech Zone/Debug ---

        public void CopyDbPath()
        {
            GUIUtility.systemCopyBuffer = Application.persistentDataPath;
        }

        public void CancelAllNotifications()
        {
            NotificationScheduler.CancelAll();
        }

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
