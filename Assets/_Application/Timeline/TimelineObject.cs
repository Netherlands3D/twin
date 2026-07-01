using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject.Properties;
using UnityEngine;

namespace Netherlands3D
{
    [RequireComponent(typeof(LayerGameObject))]
    public class TimelineObject : MonoBehaviour
    {
        LayerGameObject layerGameObject;

        void Start()
        {
            layerGameObject = GetComponent<LayerGameObject>();
            layerGameObject.InitProperty<TimelineLayerPropertyData>(layerGameObject.LayerData.LayerProperties);
        }
        
        
    }
}
