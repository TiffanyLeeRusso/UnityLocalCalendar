using UnityEngine;
using LocalCalendar.Services;

namespace LocalCalendar.Permissions
{
    public class PermissionsRequestUI : MonoBehaviour, IBackHandler
    {
        public static PermissionsRequestUI Instance;

        void Awake()
        {
            Instance = this;
            gameObject.SetActive(false);
        }

        public bool OnBackButtonPressed()
        {
            Close();
            return true; // Consumed the back button click
        }
    
        void OnEnable()
        {
            var canvas = FindFirstObjectByType<Canvas>();
            if (canvas != null)
                transform.SetParent(canvas.transform, false);
        }

        public void Show()
        {
            gameObject.SetActive(true);
            SceneHistoryManager.Instance.RegisterHandler(this);
            PlayerPrefs.SetInt("ExactAlarmPrompted", 1);
            PlayerPrefs.Save();
        }

        public void Close()
        {
            SceneHistoryManager.Instance.UnregisterHandler(this);
            gameObject.SetActive(false);
        }

        public void OnOpenSettingsPressed()
        {
            PermissionsUtils.OpenAppSettings();
            gameObject.SetActive(false);
        }
    }
}
