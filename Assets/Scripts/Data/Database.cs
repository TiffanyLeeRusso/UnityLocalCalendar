using System;
using System.IO;
using SQLite;
using UnityEngine;
using LocalCalendar.Models;

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
                if (_connection == null)
                {
                    _connection = new SQLiteConnection(DB_PATH);
                }
                return _connection;
            }
        }

        public static void Initialize()
        {
            Connection.CreateTable<CalendarItemRow>();
            Connection.CreateTable<RepeatRuleRow>();
            Connection.CreateTable<ReminderRow>();
        }

        public static void Close()
        {
            if (_connection != null)
            {
                _connection.Close();
                _connection.Dispose();
                _connection = null;
            }
        }

        public static void Flush()
        {
            if (_connection == null)
                return;

            try
            {
                _connection.Execute("PRAGMA wal_checkpoint(FULL);");
            }
            catch (Exception e)
            {
                Debug.LogWarning("Flush skipped: " + e.Message);
            }
        }

        public static void Reopen()
        {
            if (_connection == null)
                _connection = new SQLiteConnection(DB_PATH);
        }
    }
}
