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

        public RectTransform rectTransform;
        private float lastHeight;

        void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            if (textMeshPro == null)
            {
                textMeshPro = GetComponentInChildren<TextMeshProUGUI>();
            }

            UpdateHeight();
        }

        void Start()
        {
            UpdateHeight();
        }

        void OnValidate()
        {

            UpdateHeight();
        }

        public void UpdateHeight()
        {

            if (textMeshPro != null && rectTransform1 != null && rectTransform2 != null)
            {
                // Force an update of the text mesh to get the latest size  
                textMeshPro.ForceMeshUpdate();

                // Calculate the heights  
                float textHeight = textMeshPro.textBounds.size.y;
                float tallestHeight = Mathf.Max(rectTransform1.rect.height, rectTransform2.rect.height);

                // Calculate the new height, but only update if it's different  
                float newHeight = tallestHeight + additionalHeight;
                if (Mathf.Abs(newHeight - lastHeight) > Mathf.Epsilon)
                {
                    rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, newHeight);
                    lastHeight = newHeight;
                }
            }
        }
    }
}