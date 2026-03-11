using UnityEngine;
using UnityEngine.Android;
using LocalCalendar.Services;
using LocalCalendar.Utils;

namespace LocalCalendar.Permissions
{
    public static class PermissionsUtils
    {
        public static string GetPermissionsListAsString()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            string notifications = TMPUtils.ColorStatus(
                Permission.HasUserAuthorizedPermission("android.permission.POST_NOTIFICATIONS"),
                "OK",
                "Missing"
            );

            string exactAlarms = TMPUtils.ColorStatus(
                CanScheduleExactAlarms(),
                "OK",
                "Disabled"
            );

            string battery = TMPUtils.ColorStatus(
                IsIgnoringBatteryOptimizations(),
                "OK",
                "Restricted"
            );

            return
                $"Notifications: {notifications}\n" +
                $"Exact alarms: {exactAlarms}\n" +
                $"Battery unrestricted: {battery}";
#endif
            return
                $"Notifications: {TMPUtils.ColorStatus(true)}\n" +
                $"Exact alarms: {TMPUtils.ColorStatus(true)}\n" +
                $"Battery unrestricted: {TMPUtils.ColorStatus(false)}";
        }

        public static bool HasPromptedForExactAlarm()
        {
            return PlayerPrefs.GetInt("ExactAlarmPrompted", 0) == 1;
        }

        public static bool CanScheduleExactAlarms()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            using var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            using var alarmManager = activity.Call<AndroidJavaObject>(
                "getSystemService", "alarm");

            return alarmManager.Call<bool>("canScheduleExactAlarms");
#endif
            return true;
        }

        public static bool IsIgnoringBatteryOptimizations()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            using var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

            using var powerManager = activity.Call<AndroidJavaObject>(
                "getSystemService", "power");

            if (powerManager == null)
                return false;

            string packageName = activity.Call<string>("getPackageName");

            return powerManager.Call<bool>(
                "isIgnoringBatteryOptimizations", packageName);
#endif
            return true;
        }

        public static void OpenAppSettings()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            using var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

            using var intent = new AndroidJavaObject(
                "android.content.Intent",
                "android.settings.APPLICATION_DETAILS_SETTINGS");

            string pkg = activity.Call<string>("getPackageName");

            using var uriClass = new AndroidJavaClass("android.net.Uri");
            using var uri = uriClass.CallStatic<AndroidJavaObject>("fromParts", "package", pkg, null);

            intent.Call<AndroidJavaObject>("setData", uri);
            activity.Call("startActivity", intent);
#endif
        }

        // Does not always work on OneUI
        public static void OpenExactAlarmSettings()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            AndroidJavaClass unityPlayer =
                new AndroidJavaClass("com.unity3d.player.UnityPlayer");

            AndroidJavaObject activity =
                unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

            AndroidJavaObject intent =
                new AndroidJavaObject(
                    "android.content.Intent",
                    "android.settings.REQUEST_SCHEDULE_EXACT_ALARM"
                );

            activity.Call("startActivity", intent);
#endif
        }

        public static void OpenBatteryOptimizationSettings()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            using var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

            using var intent = new AndroidJavaObject(
                "android.content.Intent",
                "android.settings.REQUEST_IGNORE_BATTERY_OPTIMIZATIONS");

            string pkg = activity.Call<string>("getPackageName");

            using var uriClass = new AndroidJavaClass("android.net.Uri");
            using var uri = uriClass.CallStatic<AndroidJavaObject>("parse", "package:" + pkg);

            intent.Call<AndroidJavaObject>("setData", uri);

            activity.Call("startActivity", intent);
#endif
        }
    }
}
