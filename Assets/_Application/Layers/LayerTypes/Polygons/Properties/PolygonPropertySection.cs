using Netherlands3D.Events;
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
        
        private PolygonSelectionLayer polygonLayer;

        public PolygonSelectionLayer PolygonLayer
        {
            get => polygonLayer;
            set
            {
                polygonLayer = value;
                strokeWidthSlider.value = polygonLayer.LineWidth;
                maskToggle.isOn = polygonLayer.IsMask;
                maskInvertToggle.isOn = polygonLayer.InvertMask;

                SetLinePropertiesActive(polygonLayer.ShapeType == ShapeType.Line);
                SetGridPropertiesActive(polygonLayer.ShapeType == ShapeType.Grid);
            }
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

        private void OnEnable()
        {
            maskToggle.onValueChanged.AddListener(OnIsMaskChanged);
            maskInvertToggle.onValueChanged.AddListener(OnInvertMaskChanged);
        }

        private void OnDisable()
        {
            if (polygonLayer.ShapeType == ShapeType.Line)
                strokeWidthSlider.onValueChanged.RemoveListener(HandleStrokeWidthChange);
            if(polygonLayer.ShapeType == ShapeType.Grid)
                editGridSelectionButton.onClick.RemoveListener(ReselectLayer);
            
            maskToggle.onValueChanged.RemoveListener(OnIsMaskChanged);
            maskInvertToggle.onValueChanged.RemoveListener(OnInvertMaskChanged);
            
            EnableGridInputInEditModeEvent.InvokeStarted(false);
        }

        private void OnIsMaskChanged(bool isMask)
        {
            polygonLayer.IsMask = isMask;
        }
        
        private void OnInvertMaskChanged(bool invert)
        {
            polygonLayer.InvertMask = invert;
        }
        

        private void HandleStrokeWidthChange(float newValue)
        {
            polygonLayer.LineWidth = newValue;
        }
    }
}