using System;
using LocalCalendar.Models;

namespace LocalCalendar.Services
{
    public enum EditItemMode
    {
        Edit, // Add & edit
        Preview
    }

    public static class EditItemContext
    {
        public static string EditingItemId = null;
        public static DateTime? SelectedDate = null;
        public static EditItemMode Mode = EditItemMode.Edit;

        public static void Clear()
        {
            EditingItemId = null;
            SelectedDate = null;
            Mode = EditItemMode.Edit;
        }
    }
}
