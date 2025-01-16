using Netherlands3D.Twin.Layers.LayerTypes;
using UnityEngine;

namespace Netherlands3D.Twin.Layers.Properties
{
    public abstract class AbstractHierarchicalObjectPropertySection : MonoBehaviour
    {
        public abstract HierarchicalObjectLayerGameObject LayerGameObject { get; set; }
    }
}