using UnityEngine;

namespace Netherlands3D.Twin.Layers.Properties
{
    public abstract class PropertySectionWithLayerGameObject : MonoBehaviour //todo: delete
    {
        public virtual LayerGameObject LayerGameObject { get; set; }
    }
}