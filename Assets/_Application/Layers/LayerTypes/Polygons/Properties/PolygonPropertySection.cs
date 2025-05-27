using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D.Twin.Layers.LayerTypes.Polygons.Properties
{
    public class PolygonPropertySection : MonoBehaviour
    {
        [SerializeField] private GameObject strokeWidthTitle;
        [SerializeField] private Slider strokeWidthSlider;
        [SerializeField] private Toggle maskToggle;

        private PolygonSelectionLayer polygonLayer;

        public PolygonSelectionLayer PolygonLayer
        {
            get => polygonLayer;
            set
            {
                polygonLayer = value;
                strokeWidthSlider.value = polygonLayer.LineWidth;
                maskToggle.isOn = polygonLayer.IsMask;

                SetLinePropertiesActive(polygonLayer.ShapeType == ShapeType.Line);
            }
        }

        private void SetLinePropertiesActive(bool isLine)
        {
            strokeWidthTitle.SetActive(isLine);
            strokeWidthSlider.gameObject.SetActive(isLine);
            if (isLine)
                strokeWidthSlider.onValueChanged.AddListener(HandleStrokeWidthChange);
        }

        private void OnEnable()
        {
            maskToggle.onValueChanged.AddListener(OnIsMaskChanged);
        }

        private void OnDisable()
        {
            if (polygonLayer.ShapeType == ShapeType.Line)
                strokeWidthSlider.onValueChanged.RemoveListener(HandleStrokeWidthChange);
            maskToggle.onValueChanged.RemoveListener(OnIsMaskChanged);
        }

        private void OnIsMaskChanged(bool isMask)
        {
            var layer = isMask ? LayerMask.NameToLayer("PolygonMask") : LayerMask.NameToLayer("Projected");
            foreach (Transform t in polygonLayer.PolygonVisualisation.gameObject.transform)
            {
                t.gameObject.gameObject.layer = layer;
            }
            PolygonProjectionMask.ForceUpdateVectorsAtEndOfFrame();
        }

        private void HandleStrokeWidthChange(float newValue)
        {
            polygonLayer.LineWidth = newValue;
        }
    }
}