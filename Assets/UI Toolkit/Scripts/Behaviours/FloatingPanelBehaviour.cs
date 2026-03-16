using System.Collections.Generic;
using UnityEngine;

namespace Netherlands3D.UI.Panels
{
    public abstract class FloatingPanelBehaviour : MonoBehaviour
    {
        public abstract bool ShouldBeActive();

        public abstract FloatingPanel SpawnFloatingPanel(Vector2 screenPos, Dictionary<string,object> data = null);
    }
    
    public abstract class FloatingPanelBehaviour<T> : FloatingPanelBehaviour 
        where T : FloatingPanel, new()
    {
        public override FloatingPanel SpawnFloatingPanel(Vector2 screenPos, Dictionary<string,object> data = null)
        {
            T panel = new T();
            panel.Initialize(screenPos, data);
            return panel;
        }
    }
}
