using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace Netherlands3D.Twin
{
    public class AdjustHeightOnTextChange : MonoBehaviour
    {
        public TextMeshProUGUI textMeshPro;
        public RectTransform rectTransform1;
        public RectTransform rectTransform2;
        public float additionalHeight = 20f;

        [SerializeField] private RectTransform rectTransform;

        void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            if (textMeshPro == null)
            {
                textMeshPro = GetComponentInChildren<TextMeshProUGUI>();
            }
        }

        void Start()
        {
            UpdateHeight();
        }

        void OnValidate()
        {
            if (textMeshPro != null)
            {
                UpdateHeight();
            }
        }

        public void UpdateHeight()
        {
            if (textMeshPro != null && rectTransform1 != null && rectTransform2 != null)
            {
                textMeshPro.ForceMeshUpdate();
                float textHeight = textMeshPro.textBounds.size.y;
                float tallestHeight = Mathf.Max(rectTransform1.rect.height, rectTransform2.rect.height);
                rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, tallestHeight + additionalHeight);
            }
        }
    }
}