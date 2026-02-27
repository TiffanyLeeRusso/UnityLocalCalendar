using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

namespace LocalCalendar.Services
{
    public class TMPInputLinkHandler : MonoBehaviour, IPointerClickHandler
    {
        private TMP_InputField _inputField;
        private TMP_Text _textComponent;

        void Awake()
        {
            _inputField = GetComponent<TMP_InputField>();
            _textComponent = _inputField.textComponent;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            // Only allow clicking links if we are in ReadOnly mode or it gets weird
            if (!_inputField.readOnly) return;

            int linkIndex = TMP_TextUtilities.FindIntersectingLink(_textComponent, eventData.position, eventData.pressEventCamera);

            if (linkIndex != -1)
            {
                TMP_LinkInfo linkInfo = _textComponent.textInfo.linkInfo[linkIndex];
                string url = linkInfo.GetLinkID();
            
                // Fix for "www." links that don't have a protocol
                if (!url.StartsWith("http")) url = "https://" + url;
            
                Application.OpenURL(url);
            }
        }
    }

    /* If we ever need/want a link handler for just TMP Text, not TMP input
    public class HyperlinkHandler : MonoBehaviour, IPointerClickHandler
    {
        private TextMeshProUGUI _textMeshPro;

        void Awake() => _textMeshPro = GetComponent<TextMeshProUGUI>();

        public void OnPointerClick(PointerEventData eventData)
        {
            // Check if the click was over a link
            int linkIndex = TMP_TextUtilities.FindIntersectingLink(_textMeshPro, eventData.position, eventData.pressEventCamera);

            if (linkIndex != -1) // -1 means no link was clicked
            {
                TMP_LinkInfo linkInfo = _textMeshPro.textInfo.linkInfo[linkIndex];

                // Get the ID we put in the <link="ID"> tag
                string linkId = linkInfo.GetLinkID();

                HandleLink(linkId);
            }
        }

        private void HandleLink(string id)
        {
            if (id.StartsWith("https"))
            {
                Application.OpenURL(id);
            }
            else if (id == "id_privacy") // TODO: change this "privacy-policy" example
            {
                Debug.Log("Open Privacy Popup here!");
            }
        }
    }
    */
}

