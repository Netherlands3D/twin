using System.Collections.Generic;
using Netherlands3D.Events;
using Netherlands3D.Twin.Layers.ExtensionMethods;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.Projects;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D.Twin.Layers.LayerTypes.Polygons.Properties
{
    [PropertySection(typeof(PolygonSelectionLayerPropertyData))]
    public class PolygonPropertySection : MonoBehaviour, IVisualizationWithPropertyData
    {
        [SerializeField] private GameObject strokeWidthTitle;
        [SerializeField] private Slider strokeWidthSlider;
        [SerializeField] private Toggle maskToggle;
        [SerializeField] private Toggle maskInvertToggle;
        [SerializeField] private Button editGridSelectionButton;
        [SerializeField] private BoolEvent EnableGridInputInEditModeEvent;
        [SerializeField] private RectTransform maskToggleParent;
        [SerializeField] private MaskLayerToggle maskTogglePrefab;
        [SerializeField] private TextMeshProUGUI maxMasksText;
        private string maxMasksTextTemplate;

        private PolygonSelectionLayerPropertyData polygonPropertyData;
       
        public void LoadProperties(List<LayerPropertyData> properties)
        {
            polygonPropertyData = properties.Get<PolygonSelectionLayerPropertyData>();
            strokeWidthSlider.value = polygonPropertyData.LineWidth;
            maskToggle.isOn = polygonPropertyData.IsMask;
            maskInvertToggle.isOn = polygonPropertyData.InvertMask;

            SetLinePropertiesActive(polygonPropertyData.ShapeType == ShapeType.Line);
            SetGridPropertiesActive(polygonPropertyData.ShapeType == ShapeType.Grid);

            maskToggle.interactable = maskToggle.isOn || PolygonSelectionLayerPropertyData.NumAvailableMasks > 0;
            SetMaxMasksText();

            if (polygonPropertyData.IsMask)
                PopulateMaskLayerPanel();
        }

        private void SetMaxMasksText()
        {
            maxMasksText.text = string.Format(maxMasksTextTemplate, PolygonSelectionLayerPropertyData.NumAvailableMasks.ToString(), PolygonSelectionLayerPropertyData.MaxAvailableMasks.ToString());
        }

        private void SetLinePropertiesActive(bool isLine)
        {
            strokeWidthTitle.SetActive(isLine);
            strokeWidthSlider.gameObject.SetActive(isLine);
            if (isLine)
                strokeWidthSlider.onValueChanged.AddListener(HandleStrokeWidthChange);
        }

        private void SetGridPropertiesActive(bool isGrid)
        {
            editGridSelectionButton.transform.parent.gameObject.SetActive(isGrid);
            editGridSelectionButton.onClick.AddListener(ReselectLayer);
        }

        private void ReselectLayer()
        {           
            EnableGridInputInEditModeEvent.InvokeStarted(true);
        }

        private void Awake()
        {
            maxMasksTextTemplate = maxMasksText.text;
        }

        private void OnEnable()
        {
            maskToggle.onValueChanged.AddListener(OnIsMaskChanged);
            maskInvertToggle.onValueChanged.AddListener(OnInvertMaskChanged);
        }

        private void OnDisable()
        {            
            if (polygonPropertyData.ShapeType == ShapeType.Line)
                strokeWidthSlider.onValueChanged.RemoveListener(HandleStrokeWidthChange);
            if (polygonPropertyData.ShapeType == ShapeType.Grid)
                editGridSelectionButton.onClick.RemoveListener(ReselectLayer);

            maskToggle.onValueChanged.RemoveListener(OnIsMaskChanged);
            maskInvertToggle.onValueChanged.RemoveListener(OnInvertMaskChanged);
        }

        private void OnIsMaskChanged(bool isMask)
        {
            polygonPropertyData.IsMask = isMask;

            if (isMask)
                PopulateMaskLayerPanel();
            else
                ClearMaskLayerPanel();

            SetMaxMasksText();
        }

        private void PopulateMaskLayerPanel()
        {
            ClearMaskLayerPanel();
            foreach (var layer in ProjectData.Current.RootLayer.GetFlatHierarchy())
            {
                LayerGameObject template = ProjectData.Current.PrefabLibrary.GetPrefabById(layer.PrefabIdentifier);
                if (template != null && template.IsMaskable)
                {
                    var toggle = Instantiate(maskTogglePrefab, maskToggleParent);
                    toggle.Initialize(polygonPropertyData, layer);
                }
            }
        }

        private void ClearMaskLayerPanel()
        {
            foreach (var t in maskToggleParent.GetComponentsInChildren<MaskLayerToggle>())
            {
                Destroy(t.gameObject);
            }
        }

        private void OnInvertMaskChanged(bool invert)
        {
            polygonPropertyData.InvertMask = invert;
        }


        private void HandleStrokeWidthChange(float newValue)
        {
            polygonPropertyData.LineWidth = newValue;
        }
    }
}