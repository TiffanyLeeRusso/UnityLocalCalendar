using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Unity.Notifications.Android;
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
        [SerializeField] private TMP_Text permissionsList;
        [SerializeField] public CanvasGroup permissionsGroup; // for the fade effect to show something happened
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
            StartCoroutine(LoadPermissionsRoutine());
        }

        IEnumerator LoadPermissionsRoutine()
        {
            yield return AppUtils.Fade(permissionsGroup, 1f, 0f, 0.25f);
            yield return new WaitForSeconds(0.5f);
            permissionsList.text = PermissionsUtils.GetPermissionsListAsString();
            yield return AppUtils.Fade(permissionsGroup, 0f, 1f, 0.25f);
        }

        public void OpenCalendar()
        {
            SceneManager.LoadScene("CalendarScene");
        }

        public void RegrantPermissions()
        {
            PopupService.ShowPermissionsPopup();
        }

        public void ExportDB()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            // --- prepare export file ---
            string exportPath = Path.Combine(Application.temporaryCachePath, $"LocalCalendarBackup_{DateTime.Now:yyyyMMdd_HHmm}.db");

            try
            {
                Database.Flush();
                Database.Close();
                // Wait a tiny bit to let the OS release file handles
                System.Threading.Thread.Sleep(100);
                File.Copy(Database.DB_PATH, exportPath, true);
                using var file = new AndroidJavaObject("java.io.File", exportPath);

                LoggingService.Info(LogCategory.UI, $"[Export] File exists: {File.Exists(exportPath)}");
                LoggingService.Info(LogCategory.UI, $"[Export] Full Path: {exportPath}");

                // 1. Get the FileProvider class (Note the Capital 'P')
                using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                using var uriClass = new AndroidJavaClass("androidx.core.content.FileProvider");

                // 2. Generate the Content URI
                string pkgAuthority = activity.Call<string>("getPackageName") + ".fileprovider";
                var uri = uriClass.CallStatic<AndroidJavaObject>("getUriForFile", activity, pkgAuthority, file);

                // 3. Set up the Intent
                using var intent = new AndroidJavaObject("android.content.Intent");
                intent.Call<AndroidJavaObject>("setAction", "android.intent.action.SEND");
                intent.Call<AndroidJavaObject>("setType", "application/octet-stream");
                intent.Call<AndroidJavaObject>("putExtra", "android.intent.extra.STREAM", uri);
                int flagRead = intent.GetStatic<int>("FLAG_GRANT_READ_URI_PERMISSION");
                int flagWrite = intent.GetStatic<int>("FLAG_GRANT_WRITE_URI_PERMISSION");
                intent.Call<AndroidJavaObject>("addFlags", flagRead | flagWrite);

                // 5. Show Chooser
                // Name the share dialog instead of using the default system dialog.
                using var chooser = intent.CallStatic<AndroidJavaObject>("createChooser", intent, "Export Calendar Backup");
                activity.Call("startActivity", chooser);
            }
            catch (Exception e)
            {
                LoggingService.Error(LogCategory.UI, "DB export failed: " + e);
                return;
            }
            finally
            {
                Database.Reopen();
            }
#endif
        }

        public void ImportDB()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            using var helper = new AndroidJavaClass("com.BoxCatGames.PickerHelper");
            using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            using var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

            helper.CallStatic("launchPicker", activity);
#endif
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
