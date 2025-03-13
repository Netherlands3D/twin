using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Netherlands3D.Twin.UI.ColorPicker
{
    public class ColorWheel : MonoBehaviour
    {
        private float WheelRadius => pickerTransform.sizeDelta.x / 2;
        private static float CircumferenceInRadians => Mathf.PI * 2f;

        [SerializeField] private Color color = Color.white;
        public Color Color
        {
            get => color;
            set => SetColor(value);
        }

        [SerializeField] private Image[] colorDisplays;  
        [SerializeField] private Image pickerImage;  
        [SerializeField] private Image reticle; // The reticle to indicate color position  
        [SerializeField] private Slider opacitySlider;  
        [SerializeField] private Image maxColorValueIndication;
        [SerializeField] private Slider colorValueSlider;  
        [SerializeField] private TextMeshProUGUI hexColorText;  
        [SerializeField] private TMP_InputField hexInputField;
        [SerializeField] private Button pickColorButton;
        
        public UnityEvent<Color> colorChanged = new();

        private Texture2D pickerTexture; // To sample colors from the image  
        private RectTransform pickerTransform; // To cache calls to obtain the RectTransform  

        private void Awake()
        {
            // Cache calls
            pickerTexture = pickerImage.sprite.texture;
            pickerTransform = pickerImage.rectTransform;
        }

        private void Start()
        {
            // Initialize UI to the pre-set color.
            UpdateUI(color);
        }

        private void OnEnable()
        {
            hexInputField.onEndEdit.AddListener(SetColor);
            opacitySlider.onValueChanged.AddListener(SetOpacity);
            colorValueSlider.onValueChanged.AddListener(SetColorValue);
            pickColorButton.onClick.AddListener(PickColor);
        }

        private void OnDisable()
        {
            hexInputField.onEndEdit.RemoveListener(SetColor);
            opacitySlider.onValueChanged.RemoveListener(SetOpacity);
            colorValueSlider.onValueChanged.RemoveListener(SetColorValue);
            pickColorButton.onClick.RemoveListener(PickColor);
        }

        public void OnClickOnWheel(BaseEventData eventData)
        {
            SampleColor(eventData as PointerEventData);
        }

        public void OnDragOverWheel(BaseEventData eventData)
        {
            SampleColor(eventData as PointerEventData);
        }

        /// <summary>
        /// Sets the color to the given color value.
        /// </summary>
        public void SetColor(Color value)
        {
            SetColorWithoutNotify(value);

            colorChanged.Invoke(color);
        }

        /// <summary>
        /// Sets the color to the given color value and does not dispatch the colorChanged event.
        /// </summary>
        public void SetColorWithoutNotify(Color value)
        {
            color = value;

            UpdateUI(value);
        }

        /// <summary>
        /// Sets the color to the given hex value, this supports RRGGBB notation and RRGGBBAA notation.
        /// </summary>
        public void SetColor(string value)
        {
            if (!value.StartsWith("#"))
            {
                value = "#" + value;
            }

            if (value.Length is not (7 or 9))
            {
                Debug.LogWarning("Invalid HEX format. Ensure it is 6 or 8 characters long after '#'.");
                return;
            }

            if (!ColorUtility.TryParseHtmlString(value, out Color color))
            {
                Debug.LogWarning("Failed to parse color from hex code: " + value);
                return;
            }

            SetColor(color);
        }

        /// <summary>
        /// Sets the Hue and Saturation levels of the current color to the given values.
        /// </summary>
        public void SetHueAndSaturation(float hue, float saturation)
        {
            // Grab the color value from the currently selected color, and use that in the sampled color ..
            Color.RGBToHSV(color, out _, out _, out var value);
            
            var colorSample = Color.HSVToRGB(hue, saturation, value);
            
            // And lastly, re-apply the alpha value as well
            colorSample.a = color.a;

            SetColor(colorSample);
        }

        /// <summary>
        /// Sets the color value (darkness) of the current color to the given value.
        /// </summary>
        public void SetColorValue(float value)
        {
            Color.RGBToHSV(color, out var hue, out var saturation, out _);
            var newColor = Color.HSVToRGB(hue, saturation, value);
            newColor.a = color.a;

            SetColor(newColor);
        }

        /// <summary>
        /// Sets the opacity of the current color to the given value.
        /// </summary>
        public void SetOpacity(float value)
        {
            SetColor(new Color(color.r, color.g, color.b, value));
        }

        private void SampleColor(PointerEventData eventData)
        {
            RectTransform rectTransform = pickerImage.rectTransform;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectTransform, 
                eventData.position, 
                eventData.pressEventCamera, 
                out var localPoint
            );

            if (localPoint.magnitude > WheelRadius)
            {
                localPoint = localPoint.normalized * WheelRadius;
            }

            float u = (localPoint.x / rectTransform.sizeDelta.x) + 0.5f;
            float v = (localPoint.y / rectTransform.sizeDelta.y) + 0.5f;

            SampleColor(u, v);
        }

        private void SampleColor(float u, float v)
        {
            int x = Mathf.FloorToInt(u * pickerTexture.width);
            int y = Mathf.FloorToInt(v * pickerTexture.height);

            x = Mathf.Clamp(x, 0, pickerTexture.width - 1);
            y = Mathf.Clamp(y, 0, pickerTexture.height - 1);

            // Sample the color .. 
            var colorSample = pickerTexture.GetPixel(x, y);
            
            // but remember: it is only hue and saturation that are sampled, the other values
            // are determined by the sliders
            Color.RGBToHSV(colorSample, out var hue, out var saturation, out _);

            SetHueAndSaturation(hue, saturation);
        }

        private void UpdateUI(Color newColor)
        {
            UpdateColorWheel(newColor);

            // The color value's max should be the color's hue and saturation at max color value so that you can
            // actually slide between value 0 and 1 ..
            Color.RGBToHSV(newColor, out float h, out float s, out float v);
            maxColorValueIndication.color = Color.HSVToRGB(h, s, 1);
            
            // And then we position the knob at the actual value location along the slider
            colorValueSlider.SetValueWithoutNotify(v);
            
            // Position the knob on the opacity slider
            opacitySlider.SetValueWithoutNotify(newColor.a);
            
            foreach (var display in colorDisplays)
            {
                display.color = newColor;
            }
            UpdateHexColorText(newColor);
        }

        private void UpdateColorWheel(Color newColor)
        {
            // Convert RGB to HSV to find the correct position on the color wheel
            Color.RGBToHSV(newColor, out var hue, out var saturation, out _);

            // Calculate reticle position in pixel space using polar coordinates  
            float angle = hue * CircumferenceInRadians;
            float xPos = Mathf.Sin(angle) * WheelRadius * saturation;
            float yPos = Mathf.Cos(angle) * WheelRadius * saturation;
           
            reticle.rectTransform.anchoredPosition = new Vector2(xPos, yPos);
        }

        private void UpdateHexColorText(Color newColor)
        {
            string hex = ColorUtility.ToHtmlStringRGB(newColor);
            hexColorText.text = $"#{hex}";
            hexInputField.text = ""; // Clear the input field  
        }

        private void PickColor()
        {
            
        }
    }
}