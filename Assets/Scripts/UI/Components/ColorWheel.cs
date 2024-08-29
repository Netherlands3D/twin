using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace Netherlands3D.Twin
{
    public class ColorWheel : MonoBehaviour, IPointerDownHandler, IDragHandler
    {
        [SerializeField] private Image[] colorDisplays;      // Array to hold multiple color displays  
        [SerializeField] private Image pickerImage;          // Image for the color picker  
        [SerializeField] private Image reticle;              // The reticle to indicate color position  
        [SerializeField] private Slider opacitySlider;       // Slider for adjusting opacity  
        [SerializeField] private Slider darknessSlider;      // Slider for adjusting darkness  
        [SerializeField] private TextMeshProUGUI hexColorText; // Text element to display the HEX color code  
        [SerializeField] private TMP_InputField hexInputField; // Input field for HEX color input
        [SerializeField] private Button pickColorButton;     // Button to pick color

        private Texture2D pickerTexture;   // To sample colors from the image  
        private Color selectedColor = Color.white;

        void Start()
        {
            pickerTexture = pickerImage.sprite.texture;
            UpdateColorDisplay();
            opacitySlider.onValueChanged.AddListener(OnOpacityChanged);
            darknessSlider.onValueChanged.AddListener(OnDarknessChanged);
            UpdateReticlePosition(Vector2.zero); // Initialize reticle position  

            // Add listener to button
            pickColorButton.onClick.AddListener(PickColor);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            UpdateColor(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            UpdateColor(eventData);
        }

        private void UpdateColor(PointerEventData eventData)
        {
            RectTransform rectTransform = pickerImage.rectTransform;
            Vector2 localPoint;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, eventData.position, eventData.pressEventCamera, out localPoint);

            float radius = rectTransform.sizeDelta.x / 2;

            if (localPoint.magnitude > radius)
            {
                localPoint = localPoint.normalized * radius;
            }

            UpdateReticlePosition(localPoint);

            float u = (localPoint.x / rectTransform.sizeDelta.x) + 0.5f;
            float v = (localPoint.y / rectTransform.sizeDelta.y) + 0.5f;

            Color colorSample = SampleColor(u, v);
            selectedColor = new Color(colorSample.r, colorSample.g, colorSample.b, selectedColor.a);

            UpdateColorDisplay();
        }

        private Color SampleColor(float u, float v)
        {
            int x = Mathf.FloorToInt(u * pickerTexture.width);
            int y = Mathf.FloorToInt(v * pickerTexture.height);

            x = Mathf.Clamp(x, 0, pickerTexture.width - 1);
            y = Mathf.Clamp(y, 0, pickerTexture.height - 1);

            return pickerTexture.GetPixel(x, y);
        }

        private void UpdateColorDisplay()
        {
            float darkness = darknessSlider.value;
            Color adjustedColor = selectedColor * (1 - darkness);
            foreach (var display in colorDisplays)
            {
                display.color = new Color(adjustedColor.r, adjustedColor.g, adjustedColor.b, opacitySlider.value);
            }
            UpdateHexColorText(adjustedColor);
        }

        private void UpdateHexColorText(Color color)
        {
            string hex = ColorUtility.ToHtmlStringRGB(color);
            hexColorText.text = $"#{hex}";
            hexInputField.text = ""; // Clear the input field  
        }

        private void UpdateReticlePosition(Vector2 localPoint)
        {
            reticle.rectTransform.anchoredPosition = localPoint;
        }

        private void OnOpacityChanged(float value)
        {
            UpdateColorDisplay();
        }

        private void OnDarknessChanged(float value)
        {
            UpdateColorDisplay();
        }

        public void OnHexInputChanged()
        {
            string hex = hexInputField.text;
            // Automatically prepend '#' if it's not present  
            if (!hex.StartsWith("#"))
            {
                hex = "#" + hex;
            }

            // Ensure the input is valid and follows the correct format  
            if (hex.Length == 7 && hex.StartsWith("#"))
            {
                if (ColorUtility.TryParseHtmlString(hex, out Color color))
                {
                    selectedColor = color;

                    // Convert RGB to HSV to find the correct position on the color wheel  
                    Color.RGBToHSV(selectedColor, out float hue, out float saturation, out float value);

                    // Map the hue and saturation to the picker image's coordinate system  
                    float u = hue; // Hue as a fraction of 1  
                    float v = saturation; // Saturation as a fraction of 1

                    // Calculate reticle position in pixel space  
                    float xPos = (u * pickerImage.rectTransform.sizeDelta.x) - (pickerImage.rectTransform.sizeDelta.x / 2);
                    float yPos = (v * pickerImage.rectTransform.sizeDelta.y) - (pickerImage.rectTransform.sizeDelta.y / 2);

                    UpdateReticlePosition(new Vector2(xPos, yPos));
                    UpdateColorDisplay();
                }
                else
                {
                    Debug.LogWarning("Failed to parse color from HEX.");
                }
            }
            else
            {
                Debug.LogWarning("Invalid HEX format. Ensure it is 6 characters long after '#'.");
            }
        }

        // New method to pick color
        private void PickColor()
        {
            
        }
    }
}