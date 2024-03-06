using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Netherlands3D.Twin.UIs
{
    public class HorizontalBarGraph : MonoBehaviour
    {
        [SerializeField] private RectTransform barContainer;
        [SerializeField] private RectTransform barFill;
        [SerializeField] private TMP_Text labelText;
        [SerializeField] private TMP_Text valueText;
        [SerializeField] private Button informationButton;

        [SerializeField] private float minValue = 10;
        [SerializeField] private float maxValue = 10;

        public Button InformationButton { get => informationButton; }
        public float MaxValue { get => maxValue; set => maxValue = value; }
        public float MinValue { get => minValue; set => minValue = value; }

        private string label = "";
        [SerializeField] private float value = 0.0f;

        public Color DefaultGreen = new(0.0f, 0.8f, 0.0f);
        public Color DefaultYellow = new(0.8f, 0.8f, 0.0f);
        public Color DefaultRed = new(0.8f, 0.0f, 0.0f);


        public void SetLabel(string label)
        {
            this.label = label;
            labelText.text = label;
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
            valueText.text = value.ToString();

            if(updateFill){
                var normalisedValue = (this.value - minValue) / (maxValue - minValue);
                SetFill(normalisedValue);
            }
        }

        public void SetFill(float value)
        {
            var padFromRight = barContainer.rect.width * (Mathf.Clamp(value,0,1)-1);
            barFill.sizeDelta = new Vector2(padFromRight, barFill.sizeDelta.y);
            barFill.pivot = new Vector2(0, 0.5f);
        }

#if UNITY_EDITOR
        public void OnValidate()
        {

            if(Application.isPlaying)
                SetValue(value,true);
        }
#endif
    }
}
