using TMPro;
using UnityEngine;

namespace LocalCalendar.Prefabs
{
    public class WeekdayHeaderCell : MonoBehaviour
    {
        [SerializeField] TMP_Text label;

        public void Set(string text)
        {
            label.text = text;
        }

        public void Layout(float fontSize = 50)
        {
            label.fontSize = fontSize;
        }
    }
}
