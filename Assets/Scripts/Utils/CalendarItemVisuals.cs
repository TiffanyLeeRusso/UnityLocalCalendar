using UnityEngine;
using UnityEngine.UI;
using TMPro;
using LocalCalendar.Data;

namespace LocalCalendar.Utils
{
    public class CalendarItemVisuals : MonoBehaviour
    {
        [SerializeField] private CalendarStyleConfig styleConfig;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private TMP_Text[] textComponents;
        [SerializeField] private Image[] iconComponents;

        public void ApplyStyle(CalendarItemColor colorType)
        {
            var style = styleConfig.GetStyle(colorType);

            // Style the background/image
            if (backgroundImage != null)
            {
                if (style.isTransparent)
                {
                    backgroundImage.enabled = false;
                }
                else
                {
                    backgroundImage.enabled = true;
                    backgroundImage.sprite = style.backgroundSprite;
                    backgroundImage.color = Color.white; // Make sure images are not tinted
                }
            }

            // Style all text objects
            foreach (var txt in textComponents)
            {
                if (txt == null) continue;
                txt.color = style.textColor;
            }

            // Style all icon objects
            foreach (var img in iconComponents)
            {
                if (img == null) continue;
                img.color = style.textColor;
            }
        }
    }
}
