using UnityEngine;
using UnityEngine.EventSystems;

namespace LocalCalendar.Services
{
    public class BackgroundDimClick : MonoBehaviour, IPointerClickHandler
    {
        public GameObject popover;

        public void OnPointerClick(PointerEventData eventData)
        {
            popover.SetActive(false);
        }
    }
}
