using Netherlands3D.UI.Components;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Panels
{
    public abstract class FloatingPanel : VisualElement
    {
        public FloatingPanel()
        {
            this.CloneComponentTree("Panels");
            this.AddComponentStylesheet("Panels");
            style.position = Position.Absolute;
        }

        // Generic context object
        public virtual void Initialize(Vector2 screenPosition, object context = null)
        {
            SetPosition(screenPosition);
        }

        public void SetPosition(Vector2 screenPosition)
        {
            style.left = screenPosition.x;
            style.top = screenPosition.y;
        }
    }
}