using System;
using System.IO;
using System.Text;
using UnityEngine;
using LocalCalendar.Services;

namespace LocalCalendar.AppDebug
{
    public static class GlobalExceptionHandler
    {
        private static string LogFilePath =>
            Path.Combine(Application.persistentDataPath, "crash.log");

        public static void Init()
        {
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            Application.logMessageReceived += OnLogMessageReceived;
            Application.logMessageReceivedThreaded += (condition, stackTrace, type) =>
            {
                if (type == LogType.Exception || type == LogType.Error)
                {
                    LogCrash("THREADED_EXCEPTION", $"Type: {type.ToString()}, Condition: {condition}, StackTrace: {stackTrace}");
                }
            };
        }

        public static void Clear()
        {
            try
            {
                if (File.Exists(LogFilePath))
                    File.Delete(LogFilePath);
            }
            catch {}  // Never throw during crash handling
        }

        public static string DumpFileToString()
        {
            return LoggingService.DumpFileToString(LogFilePath, "Crash Log");
        }

        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = e.ExceptionObject as Exception;

            LogCrash("UNHANDLED_EXCEPTION", ex?.ToString() ?? "Unknown exception");
            FlushLogs();
        }

        private static void OnLogMessageReceived(
            string condition,
            string stackTrace,
            LogType type)
        {
            if (type == LogType.Exception || type == LogType.Error)
            {
                LogCrash(type.ToString(), condition + "\n" + stackTrace);
            }
        }

        private static void LogCrash(string tag, string message)
        {
            // Write to persistent file
            System.IO.File.AppendAllText(LogFilePath, $"[{DateTime.Now}] {tag}\n{message}\n\n");
        }

        private static void FlushLogs()
        {
            try
            {
                // Touch the file to force filesystem sync
                using (var fs = new FileStream(
                           LogFilePath,
                           FileMode.OpenOrCreate,
                           FileAccess.Read,
                           FileShare.ReadWrite))
                      {
                          fs.Flush(true); // true = flush to disk
                      }

                // Hack to try to guarantee something gets written
                System.IO.File.WriteAllText(
                    Path.Combine(Application.persistentDataPath, "flush.marker"),
                    DateTime.Now.ToString());
            }
            catch {} // Never throw during crash handling
        }
    }
}
