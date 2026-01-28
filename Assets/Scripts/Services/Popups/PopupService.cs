using UnityEngine;
using LocalCalendar.Permissions;

namespace LocalCalendar.Services
{
    public static class PopupService
    {
        private static PermissionsRequestUI _instance;

        public static PermissionsRequestUI ShowPermissionsPopup()
        {
            if (_instance != null)
            {
                _instance.gameObject.SetActive(true);
                _instance.Show(); // re-initialize content if needed
                return _instance;
            }

            // Instantiate as before
            var prefab = Resources.Load<PermissionsRequestUI>(
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
