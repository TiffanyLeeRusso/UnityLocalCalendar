using UnityEngine;
using TMPro;

namespace LocalCalendar.Utils
{
    public class ForceNativeInput : MonoBehaviour
    {
        private TMP_InputField _input;

        void Awake() => _input = GetComponent<TMP_InputField>();

        public void OpenNativeKeyboard()
        {
            // This manually triggers the keyboard with the 'hideInput' parameter set to false
            // (which, confusingly, is what shows the native overlay)
            TouchScreenKeyboard.Open(_input.text, TouchScreenKeyboardType.Default, false, false, false, false);
        }
    }
}

