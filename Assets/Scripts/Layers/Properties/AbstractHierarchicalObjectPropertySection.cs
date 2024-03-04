using UnityEngine;

namespace Netherlands3D.Twin.Layers.Properties
{
    public abstract class AbstractHierarchicalObjectPropertySection : MonoBehaviour, IPropertySection2
    {
        public virtual HierarchicalObjectLayer Layer { get; set; }
    }
}