using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D.Functionalities.OgcWebServices.Wms
{
    public class LegendImage : MonoBehaviour
    {        
        private Sprite sprite;
        private RectTransform rectTransform;

        public void SetSprite(Sprite sprite)
        {
            this.sprite = sprite;

            GetComponent<Image>().sprite = sprite;

            if(rectTransform == null)
                rectTransform = GetComponent<RectTransform>();

            float ar = rectTransform.rect.width / sprite.texture.width; 
            float height = rectTransform.rect.height;

            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, rectTransform.rect.width);
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, sprite.texture.height * ar);
        }
    }
}
