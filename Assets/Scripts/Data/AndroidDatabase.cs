using System;
using System.IO;
using UnityEngine;
using SQLite;
using LocalCalendar.Services;

namespace LocalCalendar.Data
{
    public static class AndroidDatabase
    {
        // --- Export DB ---

        public static bool ExportDB()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                // Get the JSON file
                string exportPath = Database.ExportToJsonFile();
                if (string.IsNullOrEmpty(exportPath)) return false;

                using var file = new AndroidJavaObject("java.io.File", exportPath);

                // Android Intent stuff
                using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                using var uriClass = new AndroidJavaClass("androidx.core.content.FileProvider");

                // Generate the Content URI
                string pkgAuthority = activity.Call<string>("getPackageName") + ".fileprovider";
                var uri = uriClass.CallStatic<AndroidJavaObject>("getUriForFile", activity, pkgAuthority, file);

                // Set up the Intent
                using var intent = new AndroidJavaObject("android.content.Intent");
                intent.Call<AndroidJavaObject>("setAction", "android.intent.action.SEND");
                intent.Call<AndroidJavaObject>("setType", "application/json"); 
                intent.Call<AndroidJavaObject>("putExtra", "android.intent.extra.STREAM", uri);
            
                int flagRead = intent.GetStatic<int>("FLAG_GRANT_READ_URI_PERMISSION");
                int flagWrite = intent.GetStatic<int>("FLAG_GRANT_WRITE_URI_PERMISSION");
                intent.Call<AndroidJavaObject>("addFlags", flagRead);

                // Show Chooser
                // Name the share dialog instead of using the default system dialog.
                using var chooser = intent.CallStatic<AndroidJavaObject>("createChooser", intent, "Export LocalCalendar Backup");
                activity.Call("startActivity", chooser);
                return true;
            }
            catch (Exception e)
            {
                LoggingService.Error(LogCategory.DB, "Android DB-export failed: " + e);
                return false;
            }
#else
            // Unity-editor value
            return true;
#endif
        }

        
        // --- ImportDB ---
        
        public static void ImportDB()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            using var helper = new AndroidJavaClass("com.BoxCatGames.PickerHelper");
            using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            using var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

            helper.CallStatic("launchPicker", activity);
#endif
        }

        public static void ImportFromUri(string uriString)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            // Temp file to store the stream from the URI
            string tempPath = Path.Combine(Application.temporaryCachePath, "import_temp.json");

            try
            {
                using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                using var uriClass = new AndroidJavaClass("android.net.Uri");
                using var uri = uriClass.CallStatic<AndroidJavaObject>("parse", uriString);
                using var resolver = activity.Call<AndroidJavaObject>("getContentResolver");

                // Single Scope for Permissions and Streams
                using (var intentClass = new AndroidJavaClass("android.content.Intent"))
                {
                    try {
                        int readFlag = intentClass.GetStatic<int>("FLAG_GRANT_READ_URI_PERMISSION");
                        resolver.Call("takePersistableUriPermission", uri, readFlag);
                    } catch { /* Non-persistable providers throw here, ignore */ }
                }

                using (var inputStream = resolver.Call<AndroidJavaObject>("openInputStream", uri))
                using (var outputStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    int totalBytesCopied = 0;

                    // Read in smaller chunks to ensure JNI stability
                    int chunkSize = 8192;

                    while (true) // "trust me" lol
                    {
                        // Call Java to read data into a NEW Java-side byte array
                        // This is more "expensive" but 100% reliable for JNI
                        byte[] javaBuffer = inputStream.Call<byte[]>("readNBytes", chunkSize);

                        if (javaBuffer == null || javaBuffer.Length == 0)
                            break;

                        // Write the returned array directly to our C# FileStream
                        outputStream.Write(javaBuffer, 0, javaBuffer.Length);
                        totalBytesCopied += javaBuffer.Length;

                        if (javaBuffer.Length < chunkSize)
                            break; // End of stream
                    }

                    outputStream.Flush(true);
                    LoggingService.Info(LogCategory.DB, $"Stream copy finished. Total bytes: {totalBytesCopied}");
                }

                // Try to import the JSON file. If it fails we assume the file is invalid.
                if (Database.ImportFromJsonFile(tempPath))
                {
                    Database.NotifyImportResult(ImportResult.Success);
                    LoggingService.Info(LogCategory.DB, "JSON Import Successful!");
                }
                else
                {
                    Database.NotifyImportResult(ImportResult.InvalidFile);
                }
            }
            catch (Exception e)
            {
                LoggingService.Error(LogCategory.DB, "Android DB-import failed: " + e.Message);
            }
            finally
            {
                if (File.Exists(tempPath)) File.Delete(tempPath);
            }
#endif
        }
    }
}
