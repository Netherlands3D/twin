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
        }

        public void SetPosition(Vector2 screenPosition)
        {
           this.screenPosition = screenPosition;
           style.left = screenPosition.x;
           style.top = screenPosition.y;
        }
    }
}