using System;
using Netherlands3D.ObjectLibrary;
using Netherlands3D.Twin.UI.LayerInspector;
using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D.Twin.Layers.Properties
{
    public class ToggleScatterPropertySection : MonoBehaviour
    {
        [SerializeField] private Toggle convertToggle;

        public ReferencedLayer Layer { get; set; }

        private void OnEnable()
        {
            convertToggle.onValueChanged.AddListener(ToggleScatter);
        }

        private void OnDisable()
        {
            convertToggle.onValueChanged.RemoveListener(ToggleScatter);
        }

        private void Start()
        {
            TogglePropertyToggle();
        }

        public void TogglePropertyToggle()
        {
            if (IsScatterLayer())
            {
                gameObject.SetActive(true);
                convertToggle.SetIsOnWithoutNotify(true);
                return;
            }
            
            if (IsObjectLayer())
            {
                if (Layer.ReferencedProxy.ParentLayer is PolygonSelectionLayer)
                {
                    gameObject.SetActive(true);
                    convertToggle.SetIsOnWithoutNotify(false);
                    return;
                }
            }

            gameObject.SetActive(false);
        }

        private void ToggleScatter(bool isOn)
        {
            if (IsScatterLayer())
            {
                var scatterLayer = Layer as ObjectScatterLayer;
                scatterLayer.RevertToHierarchicalObjectLayer();
            }
            else if (IsObjectLayer())
            {
                var objectLayer = Layer as HierarchicalObjectLayer;
                HierarchicalObjectLayer.ConvertToScatterLayer(objectLayer);
            }
        }

        public bool IsObjectLayer()
        {
            print(Layer);
            return Layer is HierarchicalObjectLayer;
        }

        public bool IsScatterLayer()
        {
            return Layer is ObjectScatterLayer;
        }
    }
}