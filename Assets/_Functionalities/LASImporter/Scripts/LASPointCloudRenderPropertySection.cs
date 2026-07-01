using System.Collections.Generic;
using Netherlands3D.Twin.Layers.ExtensionMethods;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.UI.Properties;
using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D.Functionalities.LASImporter
{
    [PropertySection(typeof(LASPointCloudRenderPropertyData))]
    public class LASPointCloudRenderPropertySection : MonoBehaviour, IVisualizationWithPropertyData
    {
        private const float MaxLoadedPointsSliderLimit = 5000000f;
        private const float MaxPointsPerChunkSliderLimit = 65000f;

        [SerializeField] private Toggle completeToggle;
        [SerializeField] private Toggle strokeToggle;
        [SerializeField] private Toggle fillToggle;
        [SerializeField] private Slider strokeWidthSlider;
        [SerializeField] private Slider densitySlider;
        [SerializeField] private Slider scatterSlider;
        [SerializeField] private Slider angleSlider;
        [SerializeField] private GameObject angleTitleLabel;
        [SerializeField] private DoubleSlider heightRangeSlider;
        [SerializeField] private DoubleSlider diameterRangeSlider;

        private LASPointCloudRenderPropertyData propertyData;
        private bool updatingPanel;

        private void Awake()
        {
            ConfigureControls();
        }

        private void OnEnable()
        {
            completeToggle.onValueChanged.AddListener(SetColorModeToFileColors);
            strokeToggle.onValueChanged.AddListener(SetColorModeToClassification);
            fillToggle.onValueChanged.AddListener(SetColorModeToSingleColor);
            strokeWidthSlider.onValueChanged.AddListener(HandlePointSizeChange);
            densitySlider.onValueChanged.AddListener(HandleReferenceDistanceChange);
            scatterSlider.onValueChanged.AddListener(HandleLodDistanceChange);
            heightRangeSlider.onMinValueChanged.AddListener(HandleMinPointSizeChange);
            heightRangeSlider.onMaxValueChanged.AddListener(HandleMaxPointSizeChange);
            diameterRangeSlider.onMinValueChanged.AddListener(HandleMaxPointsPerChunkChange);
            diameterRangeSlider.onMaxValueChanged.AddListener(HandleMaxLoadedPointsChange);
        }

        private void OnDisable()
        {
            completeToggle.onValueChanged.RemoveListener(SetColorModeToFileColors);
            strokeToggle.onValueChanged.RemoveListener(SetColorModeToClassification);
            fillToggle.onValueChanged.RemoveListener(SetColorModeToSingleColor);
            strokeWidthSlider.onValueChanged.RemoveListener(HandlePointSizeChange);
            densitySlider.onValueChanged.RemoveListener(HandleReferenceDistanceChange);
            scatterSlider.onValueChanged.RemoveListener(HandleLodDistanceChange);
            heightRangeSlider.onMinValueChanged.RemoveListener(HandleMinPointSizeChange);
            heightRangeSlider.onMaxValueChanged.RemoveListener(HandleMaxPointSizeChange);
            diameterRangeSlider.onMinValueChanged.RemoveListener(HandleMaxPointsPerChunkChange);
            diameterRangeSlider.onMaxValueChanged.RemoveListener(HandleMaxLoadedPointsChange);

            if (propertyData != null)
                propertyData.RenderSettingsChanged.RemoveListener(UpdatePanel);
        }

        public void LoadProperties(List<LayerPropertyData> properties)
        {
            if (propertyData != null)
                propertyData.RenderSettingsChanged.RemoveListener(UpdatePanel);

            propertyData = properties.Get<LASPointCloudRenderPropertyData>();
            if (propertyData == null)
                return;

            propertyData.RenderSettingsChanged.AddListener(UpdatePanel);
            UpdatePanel();
        }

        private void ConfigureControls()
        {
            strokeWidthSlider.minValue = 0.5f;
            strokeWidthSlider.maxValue = 20f;

            densitySlider.minValue = 25f;
            densitySlider.maxValue = 1000f;
            densitySlider.wholeNumbers = true;

            scatterSlider.minValue = 0.5f;
            scatterSlider.maxValue = 8f;

            heightRangeSlider.ConfigureRange(0.5f, 20f, false, 0.1f);
            diameterRangeSlider.ConfigureRange(1000f, MaxLoadedPointsSliderLimit, true, 1000f);

            if (angleSlider)
                angleSlider.gameObject.SetActive(false);

            if (angleTitleLabel)
                angleTitleLabel.SetActive(false);
        }

        private void UpdatePanel()
        {
            if (propertyData == null)
                return;

            updatingPanel = true;

            completeToggle.SetIsOnWithoutNotify(propertyData.ColorMode == LASPointColorMode.FileColors);
            strokeToggle.SetIsOnWithoutNotify(propertyData.ColorMode == LASPointColorMode.Classification);
            fillToggle.SetIsOnWithoutNotify(propertyData.ColorMode == LASPointColorMode.SingleColor);
            strokeWidthSlider.SetValueWithoutNotify(propertyData.PointSizePixels);
            densitySlider.SetValueWithoutNotify(propertyData.PointSizeReferenceDistance);
            scatterSlider.SetValueWithoutNotify(propertyData.LodDistanceMultiplier);
            heightRangeSlider.SetMinValueWithoutNotify(propertyData.MinPointSizePixels);
            heightRangeSlider.SetMaxValueWithoutNotify(propertyData.MaxPointSizePixels);
            diameterRangeSlider.SetMinValueWithoutNotify(Mathf.Min(propertyData.MaxPointsPerChunkMesh, MaxPointsPerChunkSliderLimit));
            diameterRangeSlider.SetMaxValueWithoutNotify(Mathf.Min(propertyData.MaxLoadedPoints, MaxLoadedPointsSliderLimit));

            updatingPanel = false;
        }

        private void SetColorModeToFileColors(bool isOn)
        {
            if (!isOn || updatingPanel) return;
            propertyData.ColorMode = LASPointColorMode.FileColors;
        }

        private void SetColorModeToClassification(bool isOn)
        {
            if (!isOn || updatingPanel) return;
            propertyData.ColorMode = LASPointColorMode.Classification;
        }

        private void SetColorModeToSingleColor(bool isOn)
        {
            if (!isOn || updatingPanel) return;
            propertyData.ColorMode = LASPointColorMode.SingleColor;
        }

        private void HandlePointSizeChange(float newValue)
        {
            if (updatingPanel || propertyData == null) return;
            propertyData.PointSizePixels = newValue;
        }

        private void HandleReferenceDistanceChange(float newValue)
        {
            if (updatingPanel || propertyData == null) return;
            propertyData.PointSizeReferenceDistance = newValue;
        }

        private void HandleLodDistanceChange(float newValue)
        {
            if (updatingPanel || propertyData == null) return;
            propertyData.LodDistanceMultiplier = newValue;
        }

        private void HandleMaxLoadedPointsChange(float newValue)
        {
            if (updatingPanel || propertyData == null) return;
            propertyData.MaxLoadedPoints = Mathf.RoundToInt(newValue);
        }

        private void HandleMinPointSizeChange(float newValue)
        {
            if (updatingPanel || propertyData == null) return;
            propertyData.MinPointSizePixels = newValue;
        }

        private void HandleMaxPointSizeChange(float newValue)
        {
            if (updatingPanel || propertyData == null) return;
            propertyData.MaxPointSizePixels = newValue;
        }

        private void HandleMaxPointsPerChunkChange(float newValue)
        {
            if (updatingPanel || propertyData == null) return;
            propertyData.MaxPointsPerChunkMesh = Mathf.RoundToInt(newValue);
        }
    }
}
