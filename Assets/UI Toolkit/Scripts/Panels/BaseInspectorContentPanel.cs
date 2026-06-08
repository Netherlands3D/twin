using System;
using Netherlands3D.UI;
using Netherlands3D.UI.Components;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace Netherlands3D.UI_Toolkit.Scripts.Panels
{
    public abstract class BaseInspectorContentPanel : VisualElement
    {
        public UnityEvent OnShow = new();
        public UnityEvent OnHide = new();

        public abstract string Title { get; }
        public virtual ToolbarInspector.ToolbarStyle ToolbarStyle => ToolbarInspector.ToolbarStyle.Normal;
        public void Show() => OnShow?.Invoke();
        public void Hide() => OnHide?.Invoke();
    }
}