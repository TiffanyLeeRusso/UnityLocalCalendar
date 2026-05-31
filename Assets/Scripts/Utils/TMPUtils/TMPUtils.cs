using System.Text.RegularExpressions;

namespace LocalCalendar.Utils
{
    // MAKE SURE RICH TEXT IS ENABLED!
    public static class TMPUtils
    {
        public static string ColorStatus(bool ok, string okText = "OK", string badText = "Missing")
        {
            if (ok)
                return $"<color={AppUtils.DARK_BG_OK_TEXT_COLOR}>{okText}</color>";   // green
            else
                return $"<color={AppUtils.DARK_BG_BAD_TEXT_COLOR}>{badText}</color>";  // red
        }

        // Finds http, https, and www. links
        public static string Linkify(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
        
            // Regex to find URLs
            string pattern = @"((http|https)://[^\s]+|www\.[^\s]+)";
            return Regex.Replace(text, pattern, $"<color={AppUtils.DARK_BG_LINK_COLOR}><u><link=\"$1\">$1</link></u></color>");
        }
    }
}


