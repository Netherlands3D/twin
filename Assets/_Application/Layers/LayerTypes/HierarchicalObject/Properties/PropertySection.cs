using UnityEngine;

namespace Netherlands3D.Twin.Layers.Properties
{
    public abstract class PropertySection : MonoBehaviour
    {
        public abstract void Initialize(LayerPropertyData property);
    }
}