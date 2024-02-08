using System.Collections.Generic;
using UnityEngine;

namespace Netherlands3D.Twin.Layers.LayerTypes
{
    public interface ILayerWithProperties
    {
        public List<GameObject> GetPropertySections();
    }
}