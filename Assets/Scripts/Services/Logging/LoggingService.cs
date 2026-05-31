using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using LocalCalendar.AppDebug;

namespace LocalCalendar.Services
{
    public static class LoggingService
    {
        private const int MaxEntries = 300;
        private static readonly List<AppLogEntry> _entries = new();

        private static string LogFilePath =>
            Path.Combine(Application.persistentDataPath, "applog.txt");

        // -------- PUBLIC API --------

        public static void Info(LogCategory cat, string msg)
            => Add(cat, LogLevel.Info, msg);

        public static void Warn(LogCategory cat, string msg)
            => Add(cat, LogLevel.Warning, msg);

        public static void Error(LogCategory cat, string msg)
            => Add(cat, LogLevel.Error, msg);

        public static IReadOnlyList<AppLogEntry> Entries => _entries;

        // -------- CORE --------

        private static void Add(LogCategory cat, LogLevel level, string msg)
        {
            var entry = new AppLogEntry
            {
                TimeUtc = DateTime.UtcNow,
                Category = cat,
                Level = level,
                Message = msg
            };

            _entries.Add(entry);

            if (_entries.Count > MaxEntries)
                _entries.RemoveAt(0);

            AppendToFile(entry);
            TrimFileIfNeeded();
        }

        // -------- FILE I/O --------

        private static void AppendToFile(AppLogEntry e)
        {
            try
            {
                File.AppendAllText(LogFilePath, FormatLine(e));
            }
            catch
            {
                // Swallow logging failures — NEVER crash for logging
            }
        }

        private static void TrimFileIfNeeded()
        {
            try
            {
                var lines = File.ReadAllLines(LogFilePath);
                if (lines.Length <= MaxEntries)
                    return;

                var trimmed = new string[MaxEntries];
                Array.Copy(
                    lines,
                    lines.Length - MaxEntries,
                    trimmed,
                    0,
                    MaxEntries);

                File.WriteAllLines(LogFilePath, trimmed);
            }
            catch
            {
                // Ignore
            }
        }

        private static string FormatLine(AppLogEntry e)
        {
            return
                $"{e.TimeUtc:yyyy-MM-dd HH:mm:ss}Z " +
                $"[{e.Category}] " +
                $"[{e.Level}] " +
                $"{e.Message}\n";
        }

        // -------- DIAGNOSTICS --------

        public static string DumpSessionToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== App Log ===");
            sb.AppendLine($"Entries: {_entries.Count}");
            sb.AppendLine();

            foreach (var e in _entries)
            {
                sb.AppendLine(
                    $"{e.TimeUtc:HH:mm:ss} " +
                    $"[{e.Category}] " +
                    $"[{e.Level}] " +
                    $"{e.Message}");
            }

            return sb.ToString();
        }

        public static string DumpFileToString(string path, string logName)
        {
            try
            {
                if (!File.Exists(path))
                    return $"=== {logName} ===\n(no log file found)";

                var sb = new StringBuilder();
                sb.AppendLine($"=== {logName} (from file) ===");
                sb.AppendLine($"Path: {path}");
                sb.AppendLine();

                sb.Append(File.ReadAllText(path));
                return sb.ToString();
            }
            catch (Exception ex)
            {
                return $"Failed to read log file:\n{ex}";
            }
        }

        public static string DumpAllToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine(GlobalExceptionHandler.DumpFileToString());
            sb.AppendLine();
            sb.AppendLine(DumpFileToString(LogFilePath, "App Log"));
            sb.AppendLine();
            sb.AppendLine("=== Current Session ===");
            sb.AppendLine(DumpSessionToString());
            return sb.ToString();
        }

        public static void ClearDebug()
        {
            _entries.Clear();

            try
            {
                if (File.Exists(LogFilePath))
                    File.Delete(LogFilePath);

                GlobalExceptionHandler.Clear();
            }
            catch { }
        }

        public static void ClearCrashData()
        {
            GlobalExceptionHandler.Clear();
        }
    }
}

