using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D.Twin.UI.Properties
{
    [RequireComponent(typeof(Slider))]
    public class PropertySlider : MonoBehaviour
    {
        private Slider slider;
        [SerializeField] private bool readOnly;
        [SerializeField] private TMP_Text valueField;
        [SerializeField] private TMP_InputField inputField;
        [SerializeField] private string formatString = "N2";
        [SerializeField] private string unitOfMeasurement;

        private void Awake()
        {
            slider = GetComponent<Slider>();
            valueField.gameObject.SetActive(readOnly);
            inputField.gameObject.SetActive(!readOnly);

            slider.onValueChanged.AddListener(OnValueChanged);
            inputField.onValueChanged.AddListener(OnInputValueChanged);
            OnValueChanged(slider.value);
        }

        private void OnInputValueChanged(string stringValue)
        {
            if (stringValue.EndsWith(unitOfMeasurement))
            {
                stringValue = stringValue.Substring(0, stringValue.Length - unitOfMeasurement.Length);
            }
            if (float.TryParse(stringValue, out float newValue))
            {
                slider.value = newValue;
            }
        }

        private void OnValueChanged(float value)
        {
            var valueText = value.ToString(formatString, CultureInfo.InvariantCulture) + unitOfMeasurement;
            valueField.text = valueText;
            inputField.SetTextWithoutNotify(valueText);
        }
    }
}
