using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D.Functionalities.OgcWebServices.Wms.UI
{
    public class LegendClampHeight : MonoBehaviour
    {
        private RectTransform rectTransform;
        public RectTransform TargetsParent;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
        }

        void Start()
        {            
            AdjustRectHeight(); // Adjust height initially if needed  
        }

        private void OnEnable()
        {
            // Adjust height whenever this GameObject is enabled  
            AdjustRectHeight();
        }

        public void UpdateHeight() // Call this when the layout changes  
        {
            AdjustRectHeight();
        }

        public void AdjustRectHeight()
        {
            float totalHeight = 0f;

            // Iterate through all children and sum their heights  
            foreach (RectTransform child in TargetsParent)
            {
                totalHeight += child.rect.height;
            }

            // Clamp the height between 50 and 350  
            float newHeight = Mathf.Clamp(totalHeight + 10f, 50f, 350f);

            // Set the height of the RectTransform  
            rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, newHeight);
        }
    }
}
