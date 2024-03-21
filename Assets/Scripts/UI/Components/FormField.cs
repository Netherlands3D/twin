using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Netherlands3D.Twin
{
    [RequireComponent(typeof(TMP_InputField))]
    public class FormField : MonoBehaviour
    {
        [SerializeField] private TMP_InputField inputField;

        [SerializeField] private Color activeTextColor;
        [SerializeField] private Color disabledTextColor;

        [Header("Background image")]
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Sprite activeBackgroundSprite;
        [SerializeField] private Sprite disabledBackgroundSprite;

        [Header("Optional label")]
        [SerializeField] private Image labelImage;
        [SerializeField] private Sprite activeLabelSprite;
        [SerializeField] private Sprite disabledLabelSprite;
        [SerializeField] private Color activeLabelColor;
        [SerializeField] private Color disabledLabelColor;

        public UnityEvent<string> onEndEdit => inputField.onEndEdit;
        public UnityEvent<string> onValueChanged => inputField.onValueChanged;
        public UnityEvent<string> onSubmit => inputField.onSubmit;
        public string Text { get => inputField.text; set => inputField.text = value; }

        public bool Interactable{
            get => inputField.interactable;
            set
            {
                inputField.interactable = value;

                ActiveStyling(value);
            }
        }

        private void ActiveStyling(bool value)
        {
            //Text colors
            var texts = inputField.GetComponentsInChildren<TMP_Text>();
            foreach (var text in texts)
            {
                text.color = value ? activeTextColor : disabledTextColor;
            }

            //Background images
            inputField.image.sprite = value ? activeBackgroundSprite : disabledBackgroundSprite;
            if (labelImage != null)
            {
                labelImage.sprite = value ? activeLabelSprite : disabledLabelSprite;
                labelImage.color = value ? activeLabelColor : disabledLabelColor;
            }
        }

        public void SetTextWithoutNotify(string text){
            inputField.SetTextWithoutNotify(text);
        }
    }
}
