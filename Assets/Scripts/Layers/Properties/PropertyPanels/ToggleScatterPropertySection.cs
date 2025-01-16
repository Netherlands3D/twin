using Netherlands3D.Twin.Layers.LayerTypes;
using Netherlands3D.Twin.Layers.LayerTypes.Polygons;
using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D.Twin.Layers.Properties
{
    public class ToggleScatterPropertySection : MonoBehaviour
    {
        [SerializeField] private Toggle convertToggle;

        public LayerGameObject LayerGameObject { get; set; }

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
                if (LayerGameObject.LayerData.ParentLayer is PolygonSelectionLayer)
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
                var scatterLayer = LayerGameObject as ObjectScatterLayerGameObject;
                scatterLayer.RevertToHierarchicalObjectLayer();
            }
            else if (IsObjectLayer())
            {
                var objectLayer = LayerGameObject as HierarchicalObjectLayerGameObject;
                HierarchicalObjectLayerGameObject.ConvertToScatterLayer(objectLayer);
            }
        }

        public bool IsObjectLayer()
        {
            return LayerGameObject is HierarchicalObjectLayerGameObject;
        }

        public bool IsScatterLayer()
        {
            return LayerGameObject is ObjectScatterLayerGameObject;
        }
    }
}