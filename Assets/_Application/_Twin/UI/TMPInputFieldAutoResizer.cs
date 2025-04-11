using TMPro;
using UnityEngine;

namespace Netherlands3D
{
    public class TMPInputFieldAutoResizer : MonoBehaviour
    {
        [SerializeField] private TMP_InputField inputField;
        [SerializeField] private Vector2 padding = new Vector2(0, 0);
        private RectTransform rectTransform;

#if UNITY_EDITOR
        private void OnValidate()
        {
            Resize(GetComponent<RectTransform>());
        }
#endif

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
        }

        private void Update()
        {
            Resize(rectTransform);
        }

        private void Resize(RectTransform rectTransform)
        {
            var textComponent = inputField.textComponent;
            Vector2 preferred = Vector2.zero;
            if (string.IsNullOrEmpty(inputField.text))
            {
                var placeholderText = inputField.placeholder as TMP_Text;
                if (placeholderText)
                    preferred = placeholderText.GetPreferredValues(placeholderText.text);
            }
            else
            {
                // textComponent.ForceMeshUpdate();
                preferred = textComponent.GetPreferredValues(inputField.text);
                
                // Ensure the height accounts for the number of lines including newLines
                int lineCount = textComponent.textInfo.lineCount;
                float lineHeight = textComponent.textInfo.lineInfo[0].lineHeight + textComponent.lineSpacing + textComponent.lineSpacingAdjustment;
                preferred.y = lineCount * lineHeight;
            }


            rectTransform.sizeDelta = preferred + padding;
        }
    }
}