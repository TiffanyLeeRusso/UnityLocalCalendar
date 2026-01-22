using System;
using System.IO;
using System.Collections;
using UnityEngine;
using SQLite;
using LocalCalendar.Data;
using LocalCalendar.Services;

namespace LocalCalendar.App
{
    public static class AppUtils
    {
        // --- File-import handling ---

        public static void ImportFromUri(string uriString)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            string tempPath = Path.Combine(Application.temporaryCachePath, "import_check.db");
    
            try
            {
                // 1. Copy to temp first (don't touch the live DB yet!)
                using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var uriClass = new AndroidJavaClass("android.net.Uri"))
                using (var uri = uriClass.CallStatic<AndroidJavaObject>("parse", uriString))
                using (var resolver = activity.Call<AndroidJavaObject>("getContentResolver"))
                using (var inputStream = resolver.Call<AndroidJavaObject>("openInputStream", uri))
                using (var outputStream = new FileStream(tempPath, FileMode.Create))
                {
                    byte[] buffer = new byte[8192];
                    int bytesRead;
                    while ((bytesRead = inputStream.Call<int>("read", buffer)) != -1)
                    outputStream.Write(buffer, 0, bytesRead);
                }

                // 2. Validate the file
                if (IsValidDatabase(tempPath))
                {
                    Database.Flush();
                    Database.Close();

                    // 3. Swap the files
                    if (File.Exists(Database.DB_PATH)) File.Delete(Database.DB_PATH);
                    File.Move(tempPath, Database.DB_PATH);

                    Database.Initialize();
                    LoggingService.Info(LogCategory.UI, "Import successful!");
                }
                else
                {
                    LoggingService.Error(LogCategory.UI, "Invalid file: Not a valid database.");
                    // Trigger a UI Popup here to tell the user
                }
            }
            catch (Exception e)
            {
                LoggingService.Error(LogCategory.UI, "Import failed: " + e.Message);
            }
            finally
            {
                if (File.Exists(tempPath)) File.Delete(tempPath);
            }
#endif
        }

        private static bool IsValidDatabase(string path)
        {
            try 
            {
                // Use your specific SQLite library to try and open it
                // Example: if using SQLite-net
                var db = new SQLiteConnection(path);
                db.ExecuteScalar<int>("PRAGMA integrity_check;"); 
                db.Close();
                return true; 
            }
            catch { return false; }
        }
        
        // --- UI helpers ---

        public static IEnumerator Fade(CanvasGroup g, float from, float to, float duration)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                g.alpha = Mathf.Lerp(from, to, t / duration);
                yield return null;
            }

            g.alpha = to;
        }
    }
}
