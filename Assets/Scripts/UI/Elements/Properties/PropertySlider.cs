using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D.Twin.UI.Elements.Properties
{
    [RequireComponent(typeof(Slider))]
    public class PropertySlider : MonoBehaviour
    {
        private Slider slider;
        [SerializeField] private TMP_Text valueField;
        [SerializeField] private string unitOfMeasurement;

        private void Awake()
        {
            slider = GetComponent<Slider>();
            slider.onValueChanged.AddListener(OnValueChanged);
            OnValueChanged(slider.value);
        }

        private void OnValueChanged(float value)
        {
            valueField.text = value.ToString("N2", CultureInfo.InvariantCulture) + unitOfMeasurement;
        }
    }
}
