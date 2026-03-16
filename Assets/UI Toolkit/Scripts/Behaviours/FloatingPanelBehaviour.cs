using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Panels
{
    public abstract class FloatingPanelBehaviour : MonoBehaviour
    {
        public abstract bool ShouldBeActive();
        public abstract Dictionary<string, object> GetData();
        public abstract VisualElement SpawnFloatingPanelContent(FloatingPanel panel, Dictionary<string,object> data = null);
        public abstract void Dispose();
    }
}
