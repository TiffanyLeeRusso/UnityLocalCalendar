using System;
using System.IO;
using SQLite;
using UnityEngine;
using LocalCalendar.Models;
using LocalCalendar.Services;

namespace LocalCalendar.Data
{
    public static class Database
    {
        public static string DB_PATH =
            Path.Combine(Application.persistentDataPath, "calendar.db");

        private static SQLiteConnection _connection;

        public static SQLiteConnection Connection 
        {
            get 
            {
                if (_connection == null) Open();
                return _connection;
            }
        }

        // --- Connection management ---

        public static void Open()
        {
            // 1. Ensure the old object is truly gone
            if (_connection != null)
            {
                try 
                {
                    _connection.Close();
                    _connection.Dispose();
                } 
                catch { /* ignored */ }
                _connection = null;
            }

            // 2. Force the Garbage Collector
            // Since we can't 'ClearPool' in this version of SQLite,
            // we force the GC to finalize the 
            // internal native handle belonging to the old _connection.
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();

            try 
            {
                _connection = new SQLiteConnection(DB_PATH);
        
                // We use a query that ignores the return value to avoid the 
                // library's internal "not an error" exception mapping.
                _connection.ExecuteScalar<string>("PRAGMA journal_mode=WAL;");
        
                LoggingService.Info(LogCategory.DB, "Database connection opened successfully.");
            }
            catch (Exception e)
            {
                // Check if it's the ghost error
                if (e.Message.ToLower().Contains("not an error"))
                {
                    LoggingService.Info(LogCategory.DB, "SQLite reported 'not an error' (SQLITE_OK) during startup.");
                }
                else
                {
                    LoggingService.Error(LogCategory.DB, "Database Open() error: " + e.Message);
                }
            }
        }

        public static void Initialize()
        {
            Connection.CreateTable<CalendarItemRow>();
            Connection.CreateTable<RepeatRuleRow>();
            Connection.CreateTable<ReminderRow>();
        }

        // Prepares the DB for file operations (Export/Backup).
        // Merges WAL journal into the main file and kills the connection.
        public static void ShutdownForFileAccess()
        {
            if (_connection == null) return;

            try
            {
                // 1. Merge WAL into the main .db file
                _connection.Execute("PRAGMA wal_checkpoint(FULL);");
                // 2. Switch out of WAL to ensure -wal and -shm files are deleted
                _connection.Execute("PRAGMA journal_mode=DELETE;");
            }
            catch (Exception e)
            {
                Debug.LogWarning("DB Cleanup issues (likely already closed): " + e.Message);
            }
            finally
            {
                _connection.Close();
                _connection.Dispose();
                _connection = null;
            }
        
            // Final safety: Delete orphaned journal files manually if they persist
            if (File.Exists(DB_PATH + "-wal")) File.Delete(DB_PATH + "-wal");
            if (File.Exists(DB_PATH + "-shm")) File.Delete(DB_PATH + "-shm");
        }

        // --- Import/export functions ---

        public static void ExportDB()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            AndroidDatabase.ExportDB();
#endif
        }

        public static void ImportDB()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            AndroidDatabase.ImportDB();
#endif
        }

        // ImportFromUri
        // Pull in the file and overwrite the DB
        public static void ImportFromUri(string uriString)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            AndroidDatabase.ImportFromUri(uriString);
#endif
            // Note: We do not cache data from the DB anywhere so we
            // do not need to set a calendar refresh signal here.
        }

        public static bool IsValidDatabase(string path)
        {
            try 
            {
                if (!File.Exists(path)) return false;
        
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    if (fs.Length < 16) return false;

                    byte[] header = new byte[16];
                    fs.Read(header, 0, 16);

                    // Use hex so any null bytes don't break string functions.
                    string hex = BitConverter.ToString(header).Replace("-", " ");
                    LoggingService.Info(LogCategory.DB, $"Header Hex: {hex}");

                    byte[] expected = { 0x53, 0x51, 0x4C, 0x69, 0x74, 0x65, 0x20, 0x66, 0x6F, 0x72, 0x6D, 0x61, 0x74, 0x20, 0x33, 0x00 };

                    for (int i = 0; i < 16; i++) {
                      if (header[i] != expected[i]) return false;
                    }
                    return true;
                }
            }
            catch (Exception e) {
                LoggingService.Error(LogCategory.DB, "Validation error: " + e.Message);
                return false;
            }
        }
    }
}
