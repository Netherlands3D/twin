using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Globalization;

namespace Netherlands3D.Indicators.UI
{
    public class HorizontalBarGraph : MonoBehaviour
    {
        [SerializeField] private RectTransform barContainer;
        [SerializeField] private RectTransform barFill;
        [SerializeField] private Slider bar;
        [SerializeField] private TMP_Text labelText;
        [SerializeField] private TMP_Text valueText;
        [SerializeField] private Button informationButton;

        [SerializeField] private float minValue = 10;
        [SerializeField] private float maxValue = 10;

        public Button InformationButton { get => informationButton; }
        public float MinValue { get => minValue; set => minValue = value; }
        public float MaxValue { get => maxValue; set => maxValue = value; }

        private string label = "";
        [SerializeField] private float value = 0.0f;

        public Color DefaultGreen = new(0.0f, 0.8f, 0.0f);
        public Color DefaultYellow = new(0.8f, 0.8f, 0.0f);
        public Color DefaultRed = new(0.8f, 0.0f, 0.0f);


        public void SetLabel(string label)
        {
            this.label = label;
            labelText.text = this.label;
        }

        public void SetBarColor(Color color)
        {
            if(barFill.TryGetComponent(out Image image))
                image.color = color;
            else
                Debug.LogError("No Image component found on barFill. Unable to set color.");
        }

        public void SetValue(float value , bool updateFill = true)
        {
            this.value = value;
            valueText.text = value.ToString("0.00", CultureInfo.InvariantCulture);

            if(updateFill){
                var normalisedValue = Mathf.InverseLerp(minValue, maxValue, value);
                SetFill(normalisedValue);
            }
        }

        public void SetFill(float value)
        {
            bar.value = value;
        }
    }
}
