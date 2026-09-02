using System.Collections.Generic;
using Netherlands3D.Twin.Layers.ExtensionMethods;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.UI.Components;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine;
using UnityEngine.UIElements;
using RadioButtonGroup = Netherlands3D.UI.Components.RadioButtonGroup;
using Slider = Netherlands3D.UI.Components.Slider;

namespace Netherlands3D.Functionalities.LASImporter
{
    [UxmlElement]
    [PropertySection(typeof(LASPointCloudRenderPropertyData), PropertySectionCategory.Settings)]
    public partial class LASPointCloudRenderPropertySection : VisualElement, IVisualizationWithPropertyData
    {
        private LASPointCloudRenderPropertyData propertyData;
        private bool updatingPanel;

        private readonly List<LASPointColorMode> colorModeIndices = new()
        {
            LASPointColorMode.FileColors,
            LASPointColorMode.Classification
        };

        private RadioButtonGroup colorModeRadioButtonGroup;
        private Slider pointSizeSlider;
        private Slider referenceDistanceSlider;
        private Slider pointBudgetSlider;

        public LASPointCloudRenderPropertySection()
        {
            this.CloneComponentTree("Panels");
            this.AddComponentStylesheet("Panels");

            colorModeRadioButtonGroup = this.Q<RadioButtonGroup>("Kleurmodus");
            pointSizeSlider = this.Q<Slider>("PuntGrootte");
            referenceDistanceSlider = this.Q<Slider>("ReferentieAfstand");
            pointBudgetSlider = this.Q<Slider>("PuntenBudget");

            colorModeRadioButtonGroup.RegisterValueChangedCallback(OnColorModeChanged);
            pointSizeSlider.RegisterValueChangedCallback(OnPointSizeChanged);
            referenceDistanceSlider.RegisterValueChangedCallback(OnReferenceDistanceChanged);
            pointBudgetSlider.RegisterValueChangedCallback(OnPointBudgetChanged);

            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            RemovePropertyDataListeners();
        }

        public void LoadProperties(List<LayerPropertyData> properties)
        {
            RemovePropertyDataListeners();

            propertyData = properties.Get<LASPointCloudRenderPropertyData>();
            if (propertyData == null)
                return;

            propertyData.RenderSettingsChanged.AddListener(UpdatePanel);
            propertyData.PointBudgetLimitChanged.AddListener(UpdatePanel);
            UpdatePanel();
        }

        private void RemovePropertyDataListeners()
        {
            if (propertyData == null)
                return;

            propertyData.RenderSettingsChanged.RemoveListener(UpdatePanel);
            propertyData.PointBudgetLimitChanged.RemoveListener(UpdatePanel);
        }

        private void UpdatePanel()
        {
            if (propertyData == null)
                return;

            updatingPanel = true;

            var pointBudgetLimit = propertyData.PointBudgetLimit;
            pointBudgetSlider.highValue = pointBudgetLimit;

            var colorModeIndex = colorModeIndices.IndexOf(propertyData.ColorMode);
            colorModeRadioButtonGroup.SetValueWithoutNotify(Mathf.Max(0, colorModeIndex));
            pointSizeSlider.SetValueWithoutNotify(propertyData.PointSizePixels);
            referenceDistanceSlider.SetValueWithoutNotify(propertyData.PointSizeReferenceDistance);
            pointBudgetSlider.SetValueWithoutNotify(Mathf.Clamp(propertyData.MaxLoadedPoints, 1, pointBudgetLimit));

            updatingPanel = false;
        }

        private void OnColorModeChanged(ChangeEvent<int> evt)
        {
            if (updatingPanel || propertyData == null || evt.newValue < 0 || evt.newValue >= colorModeIndices.Count)
                return;

            propertyData.ColorMode = colorModeIndices[evt.newValue];
        }

        private void OnPointSizeChanged(ChangeEvent<float> evt)
        {
            if (updatingPanel || propertyData == null) return;
            propertyData.PointSizePixels = evt.newValue;
        }

        private void OnReferenceDistanceChanged(ChangeEvent<float> evt)
        {
            if (updatingPanel || propertyData == null) return;
            propertyData.PointSizeReferenceDistance = evt.newValue;
        }

        private void OnPointBudgetChanged(ChangeEvent<float> evt)
        {
            if (updatingPanel || propertyData == null) return;
            propertyData.MaxLoadedPoints = Mathf.RoundToInt(evt.newValue);
        }
    }
}
