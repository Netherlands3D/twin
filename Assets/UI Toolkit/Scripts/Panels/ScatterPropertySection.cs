using System.Collections.Generic;
using Netherlands3D.Twin.Layers.ExtensionMethods;
using Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject.Properties;
using Netherlands3D.Twin.Layers.LayerTypes.Polygons;
using Netherlands3D.Twin.Layers.LayerTypes.Polygons.Properties;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.UI.Components;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine;
using UnityEngine.UIElements;
using RadioButtonGroup = Netherlands3D.UI.Components.RadioButtonGroup;
using Slider = UnityEngine.UIElements.Slider;

namespace Netherlands3D.UI.Panels
{
    [UxmlElement]
    [PropertySection(typeof(ToggleScatterPropertyData))]
    public partial class ScatterPropertySection : VisualElement, IVisualizationWithPropertyData
    {
        const string inactiveUSSClassName = "inactive";
        private ToggleScatterPropertyData convertToScatterPropertyData;
        private ScatterGenerationSettingsPropertyData settings;

        private VisualElement toggleScatterSection;
        private CheckboxToggle convertToggle;

        private VisualElement scatterSettingsSection;
        private RadioButtonGroup scatterAreaRadioButtonGroup;
        private Slider strokeWidthSlider;
        private Slider densitySlider;
        private Slider positionRandomnessSlider;
        private Slider rotationSlider;
        private SliderRange heightSliderRange;
        private SliderRange diameterSliderRange;

        public ScatterPropertySection()
        {
            this.CloneComponentTree("Panels");
            this.AddComponentStylesheet("Panels");

            toggleScatterSection = this.Q<VisualElement>("ToggleScatterSection");
            convertToggle = toggleScatterSection.Q<CheckboxToggle>();

            scatterSettingsSection = this.Q<VisualElement>("ScatterSettingsSection");
            scatterAreaRadioButtonGroup = scatterSettingsSection.Q<RadioButtonGroup>("LocatieVerspreiding");
            strokeWidthSlider = scatterSettingsSection.Q<Slider>("RandBreedte");
            densitySlider = scatterSettingsSection.Q<Slider>("Dichtheid");
            positionRandomnessSlider = scatterSettingsSection.Q<Slider>("Verspreidingsgraad");
            rotationSlider = scatterSettingsSection.Q<Slider>("RotatieRaster");
            heightSliderRange = scatterSettingsSection.Q<SliderRange>("HoogteVariatie");
            diameterSliderRange = scatterSettingsSection.Q<SliderRange>("DiameterVariatie");
        }

        public void LoadProperties(List<LayerPropertyData> properties)
        {
            convertToScatterPropertyData = properties.Get<ToggleScatterPropertyData>();
            SetEntireSectionVisible(convertToScatterPropertyData.AllowScatter);

            convertToScatterPropertyData.AllowScatterChanged.AddListener(SetEntireSectionVisible);
            convertToggle.RegisterValueChangedCallback(OnConvertToggleValueChanged);
            convertToggle.SetValueWithoutNotify(convertToScatterPropertyData.IsScattered);

            convertToScatterPropertyData.IsScatteredChanged.AddListener(SetScatterSettingsSectionVisible);
            SetScatterSettingsSectionVisible(convertToScatterPropertyData.IsScattered);

            settings = properties.Get<ScatterGenerationSettingsPropertyData>();
            if (settings == null)
            {
                SetSectionVisible(scatterSettingsSection, false);
                return;
            }
            
            OnScatterSettingsChanged();
            settings.ScatterSettingsChanged.AddListener(OnScatterSettingsChanged);
            settings.ScatterDistributionChanged.AddListener(OnScatterSettingsChanged);
            // settings.ScatterShapeChanged.AddListener(OnScatterSettingsChanged);
            SetRotationSliderVisible(settings.AutoRotateToLine);
            settings.AutoRotateToLineChanged.AddListener(SetRotationSliderVisible);
            
            scatterAreaRadioButtonGroup.RegisterValueChangedCallback(OnFillTypeChanged);
            strokeWidthSlider.RegisterValueChangedCallback(OnStrokeWidthValueChanged);
            densitySlider.RegisterValueChangedCallback(OnDensitySliderValueChanged);
            positionRandomnessSlider.RegisterValueChangedCallback(OnPositionRandomnessValueChanged);
            rotationSlider.RegisterValueChangedCallback(OnRotationValueChanged);
            heightSliderRange.RegisterValueChangedCallback(OnHeightRangeChanged);
            diameterSliderRange.RegisterValueChangedCallback(OnDiameterRangeChanged);
        }

        private void SetSectionVisible(VisualElement section, bool isVisible)
        {
            if (isVisible)
                section.RemoveFromClassList(inactiveUSSClassName);
            else
                section.AddToClassList(inactiveUSSClassName);
        }
        
        private void SetEntireSectionVisible(bool isVisible)
        {
            SetSectionVisible(this, isVisible);
        }
        
        private void SetRotationSliderVisible(bool autoRotate)
        {
            Debug.Log("auto rotate:  " + autoRotate);
            rotationSlider.SetEnabled(!autoRotate);
            // SetSectionVisible(rotationSlider, !autoRotate);
        }
        
        private void OnFillTypeChanged(ChangeEvent<int> evt)
        {
            switch (evt.newValue)
            {
                case 0:
                    settings.FillType = FillType.Fill;
                    break;
                case 1:
                    settings.FillType = FillType.Stroke;
                    break;
                case 2:
                    settings.FillType = FillType.Complete;
                    break;
            }
        }

        private void OnConvertToggleValueChanged(ChangeEvent<bool> evt)
        {
            convertToScatterPropertyData.IsScattered = evt.newValue;
        }

        private void SetScatterSettingsSectionVisible(bool isScattered)
        {
            SetSectionVisible(scatterSettingsSection, isScattered);
        }

        private void OnScatterSettingsChanged()
        {
            scatterAreaRadioButtonGroup.value = settings.FillType switch
            {
                FillType.Complete => 2, // Beide
                FillType.Stroke => 1, // Rand
                FillType.Fill => 0  // Midden
            };

            strokeWidthSlider.value = settings.StrokeWidth;
            densitySlider.value = settings.Density;
            positionRandomnessSlider.value = settings.Scatter;
            rotationSlider.value = settings.Angle;
            Debug.Log("settings angle: " + settings.Angle);
            Debug.Log("rot slider value: " + rotationSlider.value);
            heightSliderRange.value = new Vector2(settings.MinScale.y,  settings.MaxScale.y);
            diameterSliderRange.value = new Vector2(settings.MinScale.x, settings.MaxScale.x); //x and z are the same for diameter
        }

        private void OnStrokeWidthValueChanged(ChangeEvent<float> evt)
        {
            settings.StrokeWidth = evt.newValue;
        }
        
        private void OnDensitySliderValueChanged(ChangeEvent<float> evt)
        {
            settings.Density = evt.newValue;
        }

        private void OnPositionRandomnessValueChanged(ChangeEvent<float> evt)
        {
            settings.Scatter = evt.newValue;
        }
        
        private void OnRotationValueChanged(ChangeEvent<float> evt)
        {
            settings.Angle = evt.newValue;
        }
        
        private void OnHeightRangeChanged(ChangeEvent<Vector2> evt)
        {
            var minScale = settings.MinScale;
            var maxScale = settings.MaxScale;

            //change.x = y.min, change.y = y.max
            minScale.y = evt.newValue.x;
            maxScale.y = evt.newValue.y;

            settings.MinScale = minScale;
            settings.MaxScale = maxScale;
        }
        
        private void OnDiameterRangeChanged(ChangeEvent<Vector2> evt)
        {
            var minScale = settings.MinScale;
            var maxScale = settings.MaxScale;

            //change.x = x.min and z.min, change.y = x.max and z.max
            minScale.x = evt.newValue.x;
            maxScale.x = evt.newValue.y;
            minScale.z = evt.newValue.x;
            maxScale.z = evt.newValue.y;

            settings.MinScale = minScale;
            settings.MaxScale = maxScale;
        }
    }
}