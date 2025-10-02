using System;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class BackButton : VisualElement, IComponent
    {
        /// <summary>Raised when the back button is activated.</summary>
        public event Action Clicked;

        public BackButton()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");

            // Basic interactivity
            focusable = true;
            pickingMode = PickingMode.Position;

            // Pointer click
            RegisterCallback<ClickEvent>(_ => Clicked?.Invoke());

            // Keyboard: Space/Enter activate
            RegisterCallback<KeyDownEvent>(e =>
            {
                if (e.keyCode != KeyCode.Return && e.keyCode != KeyCode.Space) return;

                Clicked?.Invoke();
                e.StopPropagation();
            });
        }
    }
}
