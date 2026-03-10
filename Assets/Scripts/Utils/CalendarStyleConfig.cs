using UnityEngine;
using TMPro;
using LocalCalendar.Data;

namespace LocalCalendar.Utils
{
    [CreateAssetMenu(fileName = "CalendarStyleConfig", menuName = "Calendar/Style Config")]
    public class CalendarStyleConfig : ScriptableObject
    {
        [System.Serializable]
        public struct ColorStyle
        {
            public CalendarItemColor colorType;
            public Sprite backgroundSprite;
            public Color textColor;
            public bool isTransparent;
        }

        public ColorStyle[] styles;

        public ColorStyle GetStyle(CalendarItemColor type)
        {
            foreach (var style in styles)
            {
                if (style.colorType == type) return style;
            }
            return styles[0]; // Fallback to first
        }
    }
}
