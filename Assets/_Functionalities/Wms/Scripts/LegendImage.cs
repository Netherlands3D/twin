using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D.Functionalities.Wms
{
    [RequireComponent(typeof(Image))]
    public class LegendImage : MonoBehaviour
    {
        [Tooltip("De gewenste hoogte van de legenda-afbeelding in pixels.")]
        [SerializeField] private float targetHeight = 20f;

        private RectTransform rectTransform;

        public void SetSprite(Sprite sprite)
        {
            if (sprite == null || sprite.texture == null)
            {
                Debug.LogWarning("Geen sprite of texture beschikbaar voor legend image.");
                return;
            }

            // Stel de sprite in
            var image = GetComponent<Image>();
            image.sprite = sprite;

            if (rectTransform == null)
                rectTransform = GetComponent<RectTransform>();

            // Bereken breedte op basis van aspect ratio van de afbeelding
            float aspectRatio = (float)sprite.texture.width / sprite.texture.height;
            float targetWidth = targetHeight * aspectRatio;

            // Pas rectTransform aan op basis van gewenste hoogte en berekende breedte
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, targetHeight);
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, targetWidth);
        }
    }
}

