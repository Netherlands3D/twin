using System;
using System.Collections;
using System.Collections.Generic;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.UI.Elements.Properties;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Netherlands3D.Twin.Layers.Properties
{
    public class ScatterSettingsPropertySection : MonoBehaviour
    {
        private ScatterGenerationSettings settings;
        [SerializeField] private Toggle completeToggle;
        [SerializeField] private Toggle strokeToggle;
        [SerializeField] private Toggle fillToggle;
        [SerializeField] private Slider strokeWidthSlider;
        [SerializeField] private Slider densitySlider;
        [SerializeField] private Slider scatterSlider;
        [SerializeField] private Slider angleSlider;
        [SerializeField] private DoubleSlider heightRangeSlider;
        [SerializeField] private DoubleSlider diameterRangeSlider;

        public ScatterGenerationSettings Settings
        {
            get => settings;
            set
            {
                settings = value;
                densitySlider.SetValueWithoutNotify(settings.Density);
                scatterSlider.SetValueWithoutNotify(settings.Scatter);
                angleSlider.SetValueWithoutNotify(settings.Angle);
                heightRangeSlider.SetMinValueWithoutNotify(settings.MinScale.y);
                heightRangeSlider.SetMaxValueWithoutNotify(settings.MaxScale.y);
                diameterRangeSlider.SetMinValueWithoutNotify(settings.MinScale.x); //x and z are the same for diameter
                diameterRangeSlider.maxSliderValue = settings.MaxScale.x; //Only the last one should be set with regular slider.value= settings.value, to invoke the SettingsChanged event only once.
            }
        }

        private void OnEnable()
        {
            completeToggle.onValueChanged.AddListener(SetFillTypeToComplete);
            strokeToggle.onValueChanged.AddListener(SetFillTypeToStroke);
            fillToggle.onValueChanged.AddListener(SetFillTypeToFill);
            strokeWidthSlider.onValueChanged.AddListener(HandleStrokeWidthChange);
            densitySlider.onValueChanged.AddListener(HandleDensityChange);
            scatterSlider.onValueChanged.AddListener(HandleScatterChange);
            angleSlider.onValueChanged.AddListener(HandleAngleChange);
            heightRangeSlider.onMinValueChanged.AddListener(HandleMinHeightRangeChange);
            heightRangeSlider.onMaxValueChanged.AddListener(HandleMaxHeightRangeChange);
            diameterRangeSlider.onMinValueChanged.AddListener(HandleMinDiameterRangeChange);
            diameterRangeSlider.onMaxValueChanged.AddListener(HandleMaxDiameterRangeChange);
        }

        private void OnDisable()
        {
            completeToggle.onValueChanged.RemoveListener(SetFillTypeToComplete);
            strokeToggle.onValueChanged.RemoveListener(SetFillTypeToStroke);
            fillToggle.onValueChanged.RemoveListener(SetFillTypeToFill);
            strokeWidthSlider.onValueChanged.RemoveListener(HandleStrokeWidthChange);
            densitySlider.onValueChanged.RemoveListener(HandleDensityChange);
            scatterSlider.onValueChanged.RemoveListener(HandleScatterChange);
            angleSlider.onValueChanged.RemoveListener(HandleAngleChange);
            heightRangeSlider.onMinValueChanged.RemoveListener(HandleMinHeightRangeChange);
            heightRangeSlider.onMaxValueChanged.RemoveListener(HandleMaxHeightRangeChange);
            diameterRangeSlider.onMinValueChanged.RemoveListener(HandleMinDiameterRangeChange);
            diameterRangeSlider.onMinValueChanged.RemoveListener(HandleMaxDiameterRangeChange);
        }

        private void SetFillTypeToComplete(bool arg0)
        {
            settings.FillType = FillType.Complete;
        }

        private void SetFillTypeToStroke(bool arg0)
        {
            settings.FillType = FillType.Stroke;
        }
        
        private void SetFillTypeToFill(bool arg0)
        {
            settings.FillType = FillType.Fill;
        }

        private void HandleStrokeWidthChange(float newValue)
        {
            settings.StrokeWidth = newValue;
        }

        private void HandleDensityChange(float newValue)
        {
            settings.Density = newValue;
        }

        private void HandleScatterChange(float newValue)
        {
            settings.Scatter = newValue / 100f; // user sets a percentage 
        }

        private void HandleAngleChange(float newValue)
        {
            settings.Angle = newValue;
        }

        private void HandleMinHeightRangeChange(float newValue)
        {
            var minScale = settings.MinScale;
            minScale.y = newValue;
            settings.MinScale = minScale;
        }

        private void HandleMaxHeightRangeChange(float newValue)
        {
            var maxScale = settings.MaxScale;
            maxScale.y = newValue;
            settings.MaxScale = maxScale;
        }

        private void HandleMinDiameterRangeChange(float newValue)
        {
            print(settings);
            var minScale = settings.MinScale;
            minScale.x = newValue;
            minScale.z = newValue;
            settings.MinScale = minScale;
        }

        private void HandleMaxDiameterRangeChange(float newValue)
        {
            var maxScale = settings.MaxScale;
            maxScale.x = newValue;
            maxScale.z = newValue;
            settings.MaxScale = maxScale;
        }
    }
}