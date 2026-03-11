using System.Collections.Generic;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Panels
{
    public abstract class FloatingPanel : VisualElement
    {
        public UnityEvent OnClose = new();
        
        public FloatingPanel()
        {
            this.CloneComponentTree("Panels");
            this.AddComponentStylesheet("Panels");
        }

        public virtual void Initialize(Vector2 screenPosition, Dictionary<string,object> data = null)
        {
            schedule.Execute(() => SetPosition(screenPosition));
        }

        public void SetPosition(Vector2 screenPosition)
        {
            var root = panel.visualTree;

            float width = resolvedStyle.width;
            float height = resolvedStyle.height;

            float maxX = root.resolvedStyle.width - width;
            float maxY = root.resolvedStyle.height - height;

            float x = Mathf.Clamp(screenPosition.x, 0, maxX);
            float y = Mathf.Clamp(screenPosition.y, 0, maxY);

            style.left = x;
            style.top = y;
        }
    }
}