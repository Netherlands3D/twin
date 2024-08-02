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
    public class PropertyTimeSlider : MonoBehaviour
    {
        private Slider slider;
        [SerializeField] private bool readOnly;
        [SerializeField] private TMP_Text valueField;
        private const string timeFormatSpecifier = "s";
        private const int daysToSeconds = 3600 * 24;

        private void Awake()
        {
            slider = GetComponent<Slider>();
            valueField.gameObject.SetActive(readOnly);

            slider.onValueChanged.AddListener(OnValueChanged);
            OnValueChanged(slider.value);
        }

        private void OnValueChanged(float value)
        {
            int seconds = (int)value * daysToSeconds;
            DateTime now = DateTime.Now;
            DateTime point = now.AddSeconds(-seconds);
            string timeString = point.ToString(timeFormatSpecifier);
            valueField.text = timeString;
        }
    }
}
