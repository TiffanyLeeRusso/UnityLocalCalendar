using UnityEngine;

namespace LocalCalendar.Notifications
{
    public static class ExactAlarmRequest
    {
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
    }
}
