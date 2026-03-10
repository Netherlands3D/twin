using System.Collections.Generic;
using Netherlands3D.UI.Components;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Panels
{
    public abstract class FloatingPanel : VisualElement
    {
        private Dictionary<string, object> data;
        
        public UnityEvent OnClose = new();
        
        public FloatingPanel()
        {
            this.CloneComponentTree("Panels");
            this.AddComponentStylesheet("Panels");
            
            //dit in stylesheet
            style.position = Position.Absolute;
        }

        public virtual void Initialize(Vector2 screenPosition, Dictionary<string,object> data = null)
        {
            this.data = data;
            SetPosition(screenPosition);
        }

        public void SetPosition(Vector2 screenPosition)
        {
            style.left = screenPosition.x;
            style.top = screenPosition.y;
        }
    }
}