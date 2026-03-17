using System.Collections.Generic;
using Netherlands3D.Twin.Layers.Properties;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Panels
{
   
    public abstract class FloatingPanelBehaviour : ScriptableObject, IVisualizationWithPropertyData
    {
        public abstract bool ShouldBeActive();
        public abstract Dictionary<string, object> GetData();
        public abstract VisualElement SpawnFloatingPanelContent(FloatingPanel panel, Dictionary<string,object> data = null);
        public abstract void Dispose();

        //public abstract List<VisualElement> SpawnPropertyData(IVisualizationWithPropertyData data);
        public abstract void LoadProperties(List<LayerPropertyData> properties);
    }
}
