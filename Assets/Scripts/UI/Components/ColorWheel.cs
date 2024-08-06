using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace Netherlands3D.Twin
{
    public class ColorWheel : MonoBehaviour, IPointerDownHandler, IDragHandler
    {
        public Image colorDisplay;       // Display the selected color
        public Image pickerImage;        // Image for the color picker
        public Image reticle;            // The reticle to indicate color position
        public Slider opacitySlider;     // Slider for adjusting opacity
        public Slider darknessSlider;    // Slider for adjusting darkness
        public TextMeshProUGUI hexColorText;        // Text element to display the HEX color code

        private Texture2D pickerTexture; // To sample colors from the image
        private Color selectedColor = Color.white;

        void Start()
        {
            // Create a Texture2D from the picker image
            pickerTexture = pickerImage.sprite.texture;

            // Initialize values and listeners
            UpdateColorDisplay();
            opacitySlider.onValueChanged.AddListener(OnOpacityChanged);
            darknessSlider.onValueChanged.AddListener(OnDarknessChanged);
            UpdateReticlePosition(Vector2.zero); // Initialize reticle position
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            UpdateColor(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            UpdateColor(eventData);
        }

        public void OnReticleDrag(PointerEventData eventData)
        {
            UpdateColor(eventData);
        }

        private void UpdateColor(PointerEventData eventData)
        {
            RectTransform rectTransform = pickerImage.rectTransform;
            Vector2 localPoint;

            // Convert screen point to local point
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, eventData.position, eventData.pressEventCamera, out localPoint);

            // Clamp localPoint to stay within picker bounds
            float clampedX = Mathf.Clamp(localPoint.x, -rectTransform.sizeDelta.x / 2, rectTransform.sizeDelta.x / 2);
            float clampedY = Mathf.Clamp(localPoint.y, -rectTransform.sizeDelta.y / 2, rectTransform.sizeDelta.y / 2);

            // Update reticle position
            UpdateReticlePosition(new Vector2(clampedX, clampedY));

            // Convert to UV coordinates
            float u = (clampedX / rectTransform.sizeDelta.x) + 0.5f;
            float v = (clampedY / rectTransform.sizeDelta.y) + 0.5f;

            // Sample color from the texture
            Color colorSample = SampleColor(u, v);
            selectedColor = new Color(colorSample.r, colorSample.g, colorSample.b, selectedColor.a);

            UpdateColorDisplay();
        }

        private Color SampleColor(float u, float v)
        {
            // Get the pixel color from the texture at the given UV coordinates
            int x = Mathf.FloorToInt(u * pickerTexture.width);
            int y = Mathf.FloorToInt(v * pickerTexture.height);

            // Make sure we stay within the bounds of the texture
            x = Mathf.Clamp(x, 0, pickerTexture.width - 1);
            y = Mathf.Clamp(y, 0, pickerTexture.height - 1);

            return pickerTexture.GetPixel(x, y); // Sample the color
        }

        private void UpdateColorDisplay()
        {
            float darkness = darknessSlider.value;
            // Apply darkness adjustment
            Color adjustedColor = selectedColor * (1 - darkness);
            colorDisplay.color = new Color(adjustedColor.r, adjustedColor.g, adjustedColor.b, opacitySlider.value);

            // Update HEX color text
            UpdateHexColorText(adjustedColor);
        }

        private void UpdateHexColorText(Color color)
        {
            // Convert RGB to HEX
            string hex = ColorUtility.ToHtmlStringRGB(color);
            hexColorText.text = $"#{hex}"; // Set the text to display the HEX code
        }

        private void UpdateReticlePosition(Vector2 localPoint)
        {
            reticle.rectTransform.anchoredPosition = localPoint; // Set reticle position directly
        }

        private void OnOpacityChanged(float value)
        {
            UpdateColorDisplay();
        }

        private void OnDarknessChanged(float value)
        {
            UpdateColorDisplay();
        }
    }
}