using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D.Functionalities.Wms
{
    [RequireComponent(typeof(Image))]
    public class LegendImage : MonoBehaviour
    {
        [Tooltip("Schaalfactor op basis van de originele grootte van de afbeelding.")]
        [SerializeField] private float scaleFactor = 1.1f;

        private RectTransform rectTransform;

        public void SetSprite(Sprite sprite)
        {
            if (sprite == null || sprite.texture == null)
            {
                Debug.LogWarning("Geen sprite of texture beschikbaar voor legend image.");
                return;
            }

            var image = GetComponent<Image>();
            image.sprite = sprite;

            if (rectTransform == null)
                rectTransform = GetComponent<RectTransform>();

            float originalHeight = sprite.texture.height;
            float originalWidth = sprite.texture.width;

            float scaledHeight = originalHeight * scaleFactor;
            float scaledWidth = originalWidth * scaleFactor;

            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, scaledHeight);
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, scaledWidth);
        }
    }
}
