using UnityEngine;
using LocalCalendar.Permissions;
using LocalCalendar.Prefabs;

namespace LocalCalendar.Services
{
    public static class PopupService
    {
        private static PermissionsRequestPopup _instance;

        public static PermissionsRequestPopup ShowPermissionsPopup()
        {
            if (_instance != null)
            {
                _instance.gameObject.SetActive(true);
                _instance.Show(); // re-initialize content if needed
                return _instance;
            }

            var prefab = Resources.Load<PermissionsRequestPopup>(
                "UI/Popups/PermissionsRequestPopup");

            if (prefab == null)
            {
                Debug.LogError("PermissionsRequestPopup not found in Resources!");
                return null;
            }

            var canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("No Canvas found in scene!");
                return null;
            }

            _instance = Object.Instantiate(prefab, canvas.transform);
            _instance.Show();

            return _instance;
        }


        public static void Hide()
        {
            if (_instance != null)
                _instance.gameObject.SetActive(false);
        }
    }
}
