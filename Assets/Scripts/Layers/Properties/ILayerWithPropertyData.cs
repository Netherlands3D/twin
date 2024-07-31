using System.Collections.Generic;
using UnityEngine.InputSystem;

namespace Netherlands3D.Twin.Layers.Properties
{
    public interface ILayerWithPropertyData
    {
        public void LoadProperties(List<LayerPropertyData> properties);
    }
}