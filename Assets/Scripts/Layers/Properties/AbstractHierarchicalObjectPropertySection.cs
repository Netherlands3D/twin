using UnityEngine;

namespace Netherlands3D.Twin.Layers.Properties
{
    public abstract class AbstractHierarchicalObjectPropertySection : MonoBehaviour, IPropertySection
    {
        public abstract HierarchicalObjectLayerGameObject LayerGameObject { get; set; }
    }
}