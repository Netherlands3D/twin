using System.Collections.Generic;
using UnityEngine.InputSystem;

namespace Netherlands3D.Twin.Layers.Properties
{
    public interface IVisualizationWithPropertyData
    {
        public LayerPropertyData PropertyData { get; }
        public void LoadProperties(List<LayerPropertyData> properties);
    }
}