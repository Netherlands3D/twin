using Netherlands3D.UI;
using UnityEngine.UIElements;

namespace Netherlands3D.UI_Toolkit.Scripts.Panels
{
    public abstract class BaseInspectorContentPanel : VisualElement, IContainer
    {
        public abstract string GetTitle();
    }
}