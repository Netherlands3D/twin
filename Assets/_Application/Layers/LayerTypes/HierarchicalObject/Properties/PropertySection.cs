using System.Collections.Generic;
using UnityEngine;

namespace Netherlands3D.Twin.Layers.Properties
{
    //this base class is needed because an interface cannot be serialized in the registry, but a MonoBehaviour can be
    public abstract class PropertySection : MonoBehaviour, IVisualizationWithPropertyData
    { 
        public virtual void LoadProperties(List<LayerPropertyData> properties)
        {
            // LoadPropertyData(properties);
        }
        // protected abstract void LoadPropertyData(List<LayerPropertyData> properties);
    }
}