using System;
using Netherlands3D.UI;
using Netherlands3D.UI.Components;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace Netherlands3D.UI_Toolkit.Scripts.Panels
{
    public abstract class BaseInspectorContentPanel : VisualElement
    {
        public abstract string Title { get; }
        public virtual void OnInspectorClick(InspectorPanel inspector)
        {
        }
    }
}