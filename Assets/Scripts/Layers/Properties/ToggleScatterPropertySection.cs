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
            print("toggling toggle active");
            if (IsScatterLayer())
            {
                print("is scatter layer");
                convertToggle.gameObject.SetActive(true);
                convertToggle.SetIsOnWithoutNotify(true);
                return;
            }
            
            if (IsObjectLayer())
            {
                    print("is object layer");
                if (Layer.ReferencedProxy.ParentLayer is PolygonSelectionLayer)
                {
                    print("is object and has parent");
                    convertToggle.gameObject.SetActive(true);
                    convertToggle.SetIsOnWithoutNotify(false);
                    return;
                }
            }

            convertToggle.gameObject.SetActive(false);
        }

        private void ToggleScatter(bool isOn)
        {
            Debug.Log("toggle scatter " + isOn);
            if (IsScatterLayer())
            {
                print("converting to hierarchical object");
                var scatterLayer = Layer as ObjectScatterLayer;
                scatterLayer.RevertToHierarchicalObjectLayer();
            }
            else if (IsObjectLayer())
            {
                var objectLayer = Layer as HierarchicalObjectLayer;
                HierarchicalObjectLayer.ConvertToScatterLayer(objectLayer);
                print("converting to scatter");
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