using Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject;
using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D.Twin.Layers.LayerTypes.Polygons.Properties
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
            switch (LayerGameObject)
            {
                case ObjectScatterLayerGameObject:
                    gameObject.SetActive(true);
                    convertToggle.SetIsOnWithoutNotify(true);
                    return;
                case HierarchicalObjectLayerGameObject objectLayer when objectLayer.LayerData.ParentLayer is PolygonSelectionLayer:
                    gameObject.SetActive(true);
                    convertToggle.SetIsOnWithoutNotify(false);
                    return;
                default:
                    gameObject.SetActive(false);
                    break;
            }
        }

        private void ToggleScatter(bool isOn)
        {
            switch (LayerGameObject)
            {
                case ObjectScatterLayerGameObject scatterLayer:
                    App.Layers.VisualizeAs(scatterLayer.LayerData, scatterLayer.Settings.OriginalPrefabId);
                    return;
                case HierarchicalObjectLayerGameObject objectLayer:
                    App.Layers.VisualizeAs(objectLayer.LayerData, ObjectScatterLayerGameObject.ScatterBasePrefabID);
                    return;
            }
        }
    }
}