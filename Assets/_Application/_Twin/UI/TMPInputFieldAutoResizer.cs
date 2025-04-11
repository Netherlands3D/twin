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
            Vector2 preferred = textComponent.GetPreferredValues(inputField.text);
            rectTransform.sizeDelta = preferred + padding;
        }
    }
}