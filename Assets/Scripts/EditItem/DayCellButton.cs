using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LocalCalendar.EditItem
{
    public class DayCellButton : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text label;
        [SerializeField] private Sprite selectedSprite;

        private DateTime _date;
        private Action<DateTime> _onClick;
        private Sprite defaultSprite;
        private Color defaultColor;

        void Awake()
        {
            var image = GetComponent<Image>();
            defaultSprite = image.sprite;
            defaultColor = image.color;
        }

        public void Initialize(
            DateTime date,
            bool isCurrentMonth,
            bool isSelected,
            Action<DateTime> onClick)
        {
            _date = date;
            _onClick = onClick;

            label.text = date.Day.ToString();
            label.color = isCurrentMonth ? Color.white : Color.gray;

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => _onClick?.Invoke(_date));

            // Optional visuals
            button.interactable = isCurrentMonth;
            SetSelected(isSelected);
        }

        public void SetSelected(bool selected)
        {
            var image = GetComponent<Image>();
            image.sprite = selected ? selectedSprite : defaultSprite;
            // Need to change the color to white so the sprite shows up properly.
            image.color = selected ? Color.white : defaultColor;
        }
    }
}
