using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using SQLite;
using Newtonsoft.Json;
using UnityEngine;
using LocalCalendar.Models;
using LocalCalendar.Services;

namespace LocalCalendar.Data
{
    public enum ImportResult { Success, InvalidFile, Error }

    public static class Database
    {
        public static string DB_PATH =
            Path.Combine(Application.persistentDataPath, "calendar.db");
        // The UI controllers can subscribe to this to check results.
        public static event Action<ImportResult> OnImportFinished;

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
            // Ensure the old object is gone
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

            // Force the Garbage Collector
            // Since we can't 'ClearPool' in this version of SQLite,
            // we force the GC to finalize the 
            // internal native handle belonging to the old _connection.
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();

            try 
            {
                _connection = new SQLiteConnection(DB_PATH);
                RunMigrations(_connection);
        
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

        public static void Close()
        {
            if (_connection == null) return;
            try
            {
                _connection.Execute("PRAGMA wal_checkpoint(FULL);");
            }
            catch (Exception e)
            {
                if (!e.Message.ToLower().Contains("not an error"))
                    LoggingService.Error(LogCategory.DB, "Database Close() checkpoint error: " + e.Message);
            }
            try
            {
                _connection.Close();
                _connection.Dispose();
            }
            catch (Exception e)
            {
                LoggingService.Error(LogCategory.DB, "Database Close() error: " + e.Message);
            }
            finally
            {
                _connection = null;
            }
        }

        public static void Initialize()
        {
            Connection.CreateTable<CalendarItemRow>();
            Connection.CreateTable<RepeatRuleRow>();
            Connection.CreateTable<ReminderRow>();
        }

        // --- DB migrations ---

        public static void RunMigrations(SQLiteConnection db)
        {
            // Get current version (starts at 0 for new databases)
            int currentVersion = db.ExecuteScalar<int>("PRAGMA user_version;");

            // !!! On DB-Schema Change !!!

            // Step through updates

            /* --- Version 1: Initial version ---

               Since SqLite starts at v0 but other versioning systems
               (Dexie/web) do not always support v0, starting everything
               at v1 is safer/more compatible. Thus we always "migrate"
               our DB directly to v1 initially.
            */
            if (currentVersion < 1)
            {
                db.Execute("PRAGMA user_version = 1;");
            }           

            /* --- Version 2: (add explanation here) ---
             */
            /* EXAMPLE column addition: CalendarItemRow.Color
            if (currentVersion < 2)
            {
                try 
                {
                    // Check if column already exists (safeguard)
                    var tableInfo = db.GetTableInfo("CalendarItemRow");
                    if (!tableInfo.Any(c => c.Name == "Color"))
                    {
                        // Add the column
                        db.Execute("ALTER TABLE CalendarItemRow ADD COLUMN Color INTEGER DEFAULT 0;");
                    }

                    // Update version so we don't run this schema update again
                    db.Execute("PRAGMA user_version = 1;");
                    LoggingService.Info(LogCategory.DB, "DB migrated to version 1 (Color column added).");
                }
                catch (Exception ex)
                {
                    LoggingService.Error(LogCategory.DB, $"DB Migration failed: {ex.Message}");
                }
            }
            */
        }

        // --- Import/export functions ---

        internal static void NotifyImportResult(ImportResult result)
        {
            OnImportFinished?.Invoke(result);
        }

        public static bool ExportDB()
        {
            bool status = true;
#if UNITY_ANDROID && !UNITY_EDITOR
            status = AndroidDatabase.ExportDB();
#endif
            return status;
        }

        public static void ImportDB()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            AndroidDatabase.ImportDB();
#endif
        }

        public static string ExportToJsonFile()
        {
            try
            {
                CalendarRepository repo = new CalendarRepository();
                List<CalendarItem> allItems = repo.GetAllCalendarItems();

                var wrapper = new JsonDto.JsonWrapper
                {
                    Items = allItems.Select(item => new JsonDto.JsonItemDto
                    {
                        Id       = item.Id,
                        Type     = item.Type,
                        Title    = item.Title,
                        Note     = item.Note,
                        StartUtc = item.StartUtc,
                        EndUtc   = item.EndUtc,
                        AllDay   = item.AllDay,
                        Color    = item.Color,
                        RepeatRule = item.RepeatRule == null ? null : new JsonDto.JsonRepeatRuleDto
                        {
                            Interval = item.RepeatRule.Interval,
                            Unit     = item.RepeatRule.Unit,
                            UntilUtc = item.RepeatRule.UntilUtc
                        },
                        Reminders = item.Reminder == null ? null : new List<JsonDto.JsonReminderDto>
                        {
                            new JsonDto.JsonReminderDto
                            {
                                OffsetSeconds = (int)item.Reminder.Offset.TotalSeconds
                            }
                        }
                    }).ToList()
                };

                string json = JsonConvert.SerializeObject(wrapper, JsonDto.JsonSettings);
                string exportPath = Path.Combine(
                    Application.temporaryCachePath,
                    $"LocalCalendar_Backup_{DateTime.Now:yyyyMMdd_HHmm}.json");

                File.WriteAllText(exportPath, json);
                return exportPath;
            }
            catch (Exception e)
            {
                Debug.LogError("JSON Export failed: " + e.Message);
                return null;
            }
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

        public static bool ImportFromJsonFile(string path)
        {
            try
            {
                if (!File.Exists(path)) return false;
                string jsonContent = File.ReadAllText(path);

                var wrapper = JsonConvert.DeserializeObject<JsonDto.JsonWrapper>(jsonContent, JsonDto.JsonSettings);

                // !!! On DB-Schema Change !!!
                // Future legacy migration goes here before validation
                // if (wrapper.Version < 1) { ... backfill ... }

                var (valid, error) = ValidateWrapper(wrapper);
                if (!valid)
                {
                    LoggingService.Error(LogCategory.DB, $"ImportFromJsonFile: validation failed — {error}");
                    return false;
                }

                // Close and reopen cleanly before import
                // This checkpoints and flushes any pending WAL state
                Database.Close();
                Database.Open();
                Database.Initialize();

                var db = Database.Connection;
                db.BeginTransaction();
                try
                {
                    // Clear all three tables
                    db.DeleteAll<CalendarItemRow>();
                    db.DeleteAll<RepeatRuleRow>();
                    db.DeleteAll<ReminderRow>();

                    foreach (var dto in wrapper.Items)
                    {
                        db.InsertOrReplace(new CalendarItemRow
                        {
                            Id           = dto.Id,
                            Type         = (int)dto.Type,
                            Title        = dto.Title,
                            Note         = dto.Note,
                            StartUtcTicks = dto.StartUtc.Ticks,
                            EndUtcTicks  = dto.EndUtc.Ticks,
                            AllDay       = dto.AllDay ? 1 : 0,
                            Color        = (int)dto.Color
                        });

                        if (dto.RepeatRule != null)
                        {
                            db.InsertOrReplace(new RepeatRuleRow
                            {
                                ItemId        = dto.Id,
                                Interval      = dto.RepeatRule.Interval,
                                Unit          = (int)dto.RepeatRule.Unit,
                                UntilUtcTicks = dto.RepeatRule.UntilUtc?.Ticks
                            });
                        }

                        // Take first reminder only for now; schema supports one per item
                        var reminder = dto.Reminders?.FirstOrDefault();
                        if (reminder != null)
                        {
                            db.InsertOrReplace(new ReminderRow
                            {
                                ItemId        = dto.Id,
                                OffsetSeconds = reminder.OffsetSeconds
                            });
                        }
                    }

                    db.Commit();

                    // Force WAL checkpoint to merge journal into main DB file
                    // and prevent stale WAL state from corrupting subsequent connections
                    try
                    {
                        db.Execute("PRAGMA wal_checkpoint(TRUNCATE);");
                    }
                    catch (Exception ex)
                    {
                        if (!ex.Message.ToLower().Contains("not an error"))
                            LoggingService.Error(LogCategory.DB, "WAL checkpoint failed: " + ex.Message);
                        // "not an error" means SQLITE_OK, checkpoint actually succeeded
                    }

                    LoggingService.Info(LogCategory.DB, "ImportFromJsonFile: commit done");
                    return true;
                }
                catch (Exception ex)
                {
                    db.Rollback();
                    LoggingService.Error(LogCategory.DB, "SQL Import Error: " + ex.Message);
                    return false;
                }
            }
            catch (Exception e)
            {
                LoggingService.Error(LogCategory.DB, "JSON Parse Error: " + e.Message);
                return false;
            }
        }

        // ValidateWrapper
        // !!! On DB-Schema Change !!!
        private static (bool valid, string error) ValidateWrapper(JsonDto.JsonWrapper wrapper)
        {
            if (wrapper == null)
                return (false, "JSON wrapper is null");
            if (wrapper.Items == null || wrapper.Items.Count == 0)
                return (false, "No items found in JSON");

            var validUnits  = new HashSet<RepeatUnit>((RepeatUnit[])Enum.GetValues(typeof(RepeatUnit)));
            var validTypes  = new HashSet<CalendarItemType>((CalendarItemType[])Enum.GetValues(typeof(CalendarItemType)));
            var validColors = new HashSet<CalendarItemColor>((CalendarItemColor[])Enum.GetValues(typeof(CalendarItemColor)));

            var seenIds = new HashSet<string>();
            var minDate = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var maxDate = new DateTime(2100, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            for (int i = 0; i < wrapper.Items.Count; i++)
            {
                var item = wrapper.Items[i];
                string ctx = $"Item[{i}] '{item?.Title ?? "null"}'";

                if (item == null)
                    return (false, $"{ctx}: item is null");
                if (string.IsNullOrWhiteSpace(item.Id))
                    return (false, $"{ctx}: missing Id");
                if (!seenIds.Add(item.Id))
                    return (false, $"{ctx}: duplicate Id '{item.Id}'");
                if (!validTypes.Contains(item.Type))
                    return (false, $"{ctx}: invalid Type '{item.Type}'");
                if (!validColors.Contains(item.Color))
                    return (false, $"{ctx}: invalid Color '{item.Color}'");
                if (item.StartUtc == default || item.StartUtc < minDate || item.StartUtc > maxDate)
                    return (false, $"{ctx}: invalid StartUtc '{item.StartUtc}'");
                if (item.EndUtc == default || item.EndUtc < minDate || item.EndUtc > maxDate)
                    return (false, $"{ctx}: invalid EndUtc '{item.EndUtc}'");
                if (item.EndUtc < item.StartUtc)
                    return (false, $"{ctx}: EndUtc is before StartUtc");

                if (item.RepeatRule != null)
                {
                    var rule = item.RepeatRule;
                    if (rule.Interval <= 0)
                        return (false, $"{ctx}: RepeatRule.Interval must be > 0, got {rule.Interval}");
                    if (!validUnits.Contains(rule.Unit))
                        return (false, $"{ctx}: invalid RepeatRule.Unit '{rule.Unit}'");
                    if (rule.UntilUtc.HasValue)
                    {
                        if (rule.UntilUtc.Value < item.StartUtc)
                            return (false, $"{ctx}: RepeatRule.UntilUtc is before StartUtc");
                        if (rule.UntilUtc.Value > maxDate)
                            return (false, $"{ctx}: RepeatRule.UntilUtc out of range");
                    }
                }

                if (item.Reminders != null)
                {
                    for (int r = 0; r < item.Reminders.Count; r++)
                    {
                        var rem = item.Reminders[r];
                        if (rem.OffsetSeconds < 0)
                            return (false, $"{ctx}: Reminders[{r}].OffsetSeconds cannot be negative");
                        if (rem.OffsetSeconds > 60 * 60 * 24 * 30)
                            return (false, $"{ctx}: Reminders[{r}].OffsetSeconds suspiciously large");
                    }
                }
            }

            return (true, null);
        }


        // --- (old code) ---

        // When we were exporting/importing SqLite .db files directly these functions
        // were needed. Now we just use a JSON format because JSON is more universal.

        /*
        // Prepares the DB for file operations.
        // Merges WAL journal into the main file and kills the connection.
        public static void ShutdownForFileAccess()
        {
            if (_connection == null) return;

            try
            {
                // Merge WAL into the main .db file
                _connection.Execute("PRAGMA wal_checkpoint(FULL);");
                // Switch out of WAL to ensure -wal and -shm files are deleted
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

        // Validate the .db file.
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
        */
    }
}
