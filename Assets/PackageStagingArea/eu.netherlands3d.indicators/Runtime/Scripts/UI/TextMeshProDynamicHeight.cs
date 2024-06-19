using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Netherlands3D.Indicators.UI
{
    [RequireComponent(typeof(RectTransform), typeof(TextMeshProUGUI))]
    public class TextMeshProDynamicHeight : MonoBehaviour
    {
        private RectTransform rectTransform;
        private TextMeshProUGUI textMeshPro;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            textMeshPro = GetComponent<TextMeshProUGUI>();
        }

        private void Update()
        {
            rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, textMeshPro.preferredHeight);
        }
    }
}
