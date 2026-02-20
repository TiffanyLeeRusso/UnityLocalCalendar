using UnityEngine;
using TMPro;

public class MobileInputHelper : MonoBehaviour
{
    private TMP_InputField _inputField;

    void Awake() => _inputField = GetComponent<TMP_InputField>();

    // Call this from a "Copy" button or a long-press event
    public void CopyToClipboard()
    {
        GUIUtility.systemCopyBuffer = _inputField.text;
        Debug.Log("Copied to clipboard!");
    }

    // Call this from a "Paste" button
    public void PasteFromClipboard()
    {
        _inputField.text = GUIUtility.systemCopyBuffer;
    }
}
