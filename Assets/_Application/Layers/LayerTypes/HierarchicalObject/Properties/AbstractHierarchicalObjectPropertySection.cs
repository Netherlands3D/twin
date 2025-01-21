using UnityEngine;

namespace Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject.Properties
{
    public abstract class AbstractHierarchicalObjectPropertySection : MonoBehaviour
    {
        public abstract HierarchicalObjectLayerGameObject LayerGameObject { get; set; }
    }
}