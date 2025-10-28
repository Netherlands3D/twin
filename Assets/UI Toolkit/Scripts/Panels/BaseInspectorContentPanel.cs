using System;
using Netherlands3D.UI;
using Netherlands3D.UI.Components;
using UnityEngine.UIElements;

namespace Netherlands3D.UI_Toolkit.Scripts.Panels
{
    public abstract class BaseInspectorContentPanel : VisualElement
    {
        public Action OnShow;
        public Action OnHide;

        public abstract string GetTitle();
        public virtual ToolbarInspector.ToolbarStyle ToolbarStyle => ToolbarInspector.ToolbarStyle.Normal;
        public void Show() => OnShow?.Invoke();
        public void Hide() => OnHide?.Invoke();
    }
}