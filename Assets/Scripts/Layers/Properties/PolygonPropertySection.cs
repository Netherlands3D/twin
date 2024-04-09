using System.Collections;
using System.Collections.Generic;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.Properties;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Netherlands3D.Twin
{
    public class PolygonPropertySection : MonoBehaviour
    {
        [SerializeField] private Slider strokeWidthSlider;

        private PolygonSelectionLayer polygonLayer;
        public PolygonSelectionLayer PolygonLayer
        {
            get => polygonLayer;
            set
            {
                polygonLayer = value;
                strokeWidthSlider.value = polygonLayer.LineWidth;
            }
        }

        private void OnEnable()
        {
            strokeWidthSlider.onValueChanged.AddListener(HandleStrokeWidthChange);
        }
        
        private void OnDisable()
        {
            strokeWidthSlider.onValueChanged.RemoveListener(HandleStrokeWidthChange);
        }

        private void HandleStrokeWidthChange(float newValue)
        {
            polygonLayer.LineWidth = newValue;
        }
    }
}
