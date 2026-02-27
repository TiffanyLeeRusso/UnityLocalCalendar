using System.Text.RegularExpressions;

namespace LocalCalendar.Services
{
    // MAKE SURE RICH TEXT IS ENABLED!
    public static class TMPUtils
    {
        public static string LINK_COLOR = "#82B1FF";

        public static string ColorStatus(bool ok, string okText = "OK", string badText = "Missing")
        {
            if (ok)
                return $"<color=#4CAF50>{okText}</color>";   // green
            else
                return $"<color=#F44336>{badText}</color>";  // red
        }

        // Finds http, https, and www. links
        public static string Linkify(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
        
            // Regex to find URLs
            string pattern = @"((http|https)://[^\s]+|www\.[^\s]+)";
            return Regex.Replace(text, pattern, $"<color={LINK_COLOR}><u><link=\"$1\">$1</link></u></color>");
        }
    }
}


