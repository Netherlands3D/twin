using System.Collections.Generic;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Panels
{
    public class FloatingElement : VisualElement
    {
 
        private Vector2 screenPosition;
        
        public FloatingElement()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");
            
            schedule.Execute(() =>
            {
                style.position = Position.Absolute; //todo: move to css if possible?
            });
            
            RegisterCallback<GeometryChangedEvent>(ClampViewPort);
        }

        private void ClampViewPort(GeometryChangedEvent evt)
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

        public void SetPosition(Vector2 screenPosition)
        {
           this.screenPosition = screenPosition;
           style.left = screenPosition.x;
           style.top = screenPosition.y;
        }
    }
}