using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace LocalCalendar.Services
{
    public class TMPForceFocus : MonoBehaviour, IPointerDownHandler
    {
        TMP_InputField input;

        void Awake()
        {
            input = GetComponent<TMP_InputField>();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            input.ActivateInputField();
            input.caretPosition = input.text.Length;
        }
    }
}
