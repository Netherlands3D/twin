using System.Collections.Generic;
using Netherlands3D.Twin.Layers.Properties;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Panels
{
   
    public abstract class FloatingPanelBehaviour : ScriptableObject
    {
        protected FloatingPanel floatingPanel;
        protected VisualElement content;
        
        public abstract bool ShouldBeActive();
        public abstract Dictionary<string, object> GetData();

        public virtual VisualElement SpawnFloatingPanelContent(FloatingPanel panel, Dictionary<string, object> data = null)
        {
            floatingPanel = panel;
            return content;
        }

        public virtual void Dispose()
        {
            content = null;
            floatingPanel = null;
        }
    }
}
