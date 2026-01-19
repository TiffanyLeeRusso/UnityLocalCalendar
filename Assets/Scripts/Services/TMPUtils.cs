
namespace LocalCalendar.Services
{
    // MAKE SURE RICH TEXT IS ENABLED!
    public static class TMPUtils
    {
        public static string ColorStatus(bool ok, string okText = "OK", string badText = "Missing")
        {
            if (ok)
                return $"<color=#4CAF50>{okText}</color>";   // green
            else
                return $"<color=#F44336>{badText}</color>";  // red
        }
    }
}


