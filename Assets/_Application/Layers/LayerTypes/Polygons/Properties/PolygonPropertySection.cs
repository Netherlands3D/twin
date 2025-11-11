using System;
using Netherlands3D.Events;
using Netherlands3D.SelectionTools;
using Netherlands3D.Twin.Projects;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D.Twin.Layers.LayerTypes.Polygons.Properties
{
    public class PolygonPropertySection : MonoBehaviour
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

        private LayerData polygonLayer;

        public LayerData PolygonLayer
        {
            get => polygonLayer;
            set
            {
                polygonLayer = value;
                PolygonSelectionLayerPropertyData data = polygonLayer.GetProperty<PolygonSelectionLayerPropertyData>();
                strokeWidthSlider.value = data.LineWidth;
                maskToggle.isOn = data.IsMask;
                maskInvertToggle.isOn = data.InvertMask;

                SetLinePropertiesActive(data.ShapeType == ShapeType.Line);
                SetGridPropertiesActive(data.ShapeType == ShapeType.Grid);

                maskToggle.interactable = maskToggle.isOn || PolygonSelectionVisualisation.NumAvailableMasks > 0;
                SetMaxMasksText();

                if (data.IsMask)
                    PopulateMaskLayerPanel();
            }
        }

        private void SetMaxMasksText()
        {
            maxMasksText.text = string.Format(maxMasksTextTemplate, PolygonSelectionVisualisation.NumAvailableMasks.ToString(), PolygonSelectionVisualisation.MaxAvailableMasks.ToString());
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
            polygonLayer.SelectLayer(true);
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
            PolygonSelectionLayerPropertyData data = polygonLayer.GetProperty<PolygonSelectionLayerPropertyData>();
            if (data.ShapeType == ShapeType.Line)
                strokeWidthSlider.onValueChanged.RemoveListener(HandleStrokeWidthChange);
            if (data.ShapeType == ShapeType.Grid)
                editGridSelectionButton.onClick.RemoveListener(ReselectLayer);

            maskToggle.onValueChanged.RemoveListener(OnIsMaskChanged);
            maskInvertToggle.onValueChanged.RemoveListener(OnInvertMaskChanged);

            EnableGridInputInEditModeEvent.InvokeStarted(false);
        }

        private void OnIsMaskChanged(bool isMask)
        {
            PolygonSelectionLayerPropertyData data = polygonLayer.GetProperty<PolygonSelectionLayerPropertyData>();
            data.IsMask = isMask;

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
                    toggle.Initialize(polygonLayer, layer);
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
            PolygonSelectionLayerPropertyData data = polygonLayer.GetProperty<PolygonSelectionLayerPropertyData>();
            data.InvertMask = invert;
        }


        private void HandleStrokeWidthChange(float newValue)
        {
            PolygonSelectionLayerPropertyData data = polygonLayer.GetProperty<PolygonSelectionLayerPropertyData>();
            data.LineWidth = newValue;
        }
    }
}