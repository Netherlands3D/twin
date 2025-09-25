using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class BackButton : VisualElement
    {
        /// <summary>Raised when the back button is activated.</summary>
        public event Action Clicked;

        public BackButton()
        {
            // Find and load UXML template for this component
            var vta = Resources.Load<VisualTreeAsset>("UI/" + nameof(BackButton));
            vta.CloneTree(this);

            // Find and load USS stylesheet specific for this component (using -style)
            var ss = Resources.Load<StyleSheet>("UI/" + nameof(BackButton) + "-style");
            styleSheets.Add(ss);

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
