using System;
using LocalCalendar.Models;

namespace LocalCalendar.App
{
    public static class EditItemContext
    {
        public static string EditingItemId = null;
        public static DateTime? SelectedDate = null;

        public static void Clear()
        {
            EditingItemId = null;
            SelectedDate = null;
        }
    }
}
