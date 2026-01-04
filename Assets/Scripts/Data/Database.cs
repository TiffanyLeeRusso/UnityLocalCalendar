using System.IO;
using SQLite;
using UnityEngine;
using LocalCalendar.Models;

namespace LocalCalendar.Data
{
    public static class Database
    {
        private static SQLiteConnection _connection;

        public static SQLiteConnection Connection
        {
            get
            {
                if (_connection == null)
                {
                    string path = Path.Combine(
                        Application.persistentDataPath,
                        "calendar.db");

                    _connection = new SQLiteConnection(path);
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
    }
}
