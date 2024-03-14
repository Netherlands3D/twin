using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D.Twin
{
    public class ScatterSettingsPropertySection : MonoBehaviour
    {
        private ScatterGenerationSettings settings;
        [SerializeField] private Slider densitySlider;
        [SerializeField] private Slider scatterSlider;
        [SerializeField] private Slider angleSlider;

        public ScatterGenerationSettings Settings
        {
            get => settings;
            set
            {
                settings = value;
                densitySlider.SetValueWithoutNotify(settings.Density); 
                scatterSlider.SetValueWithoutNotify(settings.Scatter);
                angleSlider.value = settings.Angle; //Only the last one should be set with regular slider.value= settings.value, to invoke the SettingsChanged event only once.
            }
        }

        private void OnEnable()
        {
            densitySlider.onValueChanged.AddListener(HandleDensityChange);
            scatterSlider.onValueChanged.AddListener(HandleScatterChange);
            angleSlider.onValueChanged.AddListener(HandleAngleChange);
        }

        private void OnDisable()
        {
            densitySlider.onValueChanged.RemoveListener(HandleDensityChange);
            scatterSlider.onValueChanged.RemoveListener(HandleScatterChange);
            angleSlider.onValueChanged.RemoveListener(HandleAngleChange);
        }

        private void HandleDensityChange(float newValue)
        {
            settings.Density = newValue;
        }
        
        private void HandleScatterChange(float newValue)
        {
            settings.Scatter = newValue/100f; // user sets a percentage 
        }

        private void HandleAngleChange(float newValue)
        {
            settings.Angle = newValue;
        }
    }
}
