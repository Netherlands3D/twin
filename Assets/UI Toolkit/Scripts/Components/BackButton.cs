using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    /// <summary>
    /// Small back button used inside Breadcrumb.
    /// - Loads UXML/USS from Resources/UI.
    /// - Exposes a Route string and a Clicked event.
    /// - Uses NL3D Icon for the glyph; image is resolved via CSS by default.
    /// </summary>
    [UxmlElement]
    public partial class BackButton : VisualElement
    {
        private Icon Icon => this.Q<Icon>("Icon");

        /// <summary>Optional route or token the host can use for navigation.</summary>
        [UxmlAttribute("route")]
        public string Route { get; set; }

        /// <summary>Raised when the back button is activated.</summary>
        public event Action<string> Clicked;

        public BackButton()
        {
            // Find and load UXML template for this component
            var vta = Resources.Load<VisualTreeAsset>("UI/" + nameof(BackButton));
            vta.CloneTree(this);

            // Find and load USS stylesheet specific for this component (using -style)
            var ss = Resources.Load<StyleSheet>("UI/" + nameof(BackButton) + "-style");
            styleSheets.Add(ss);

            AddToClassList("backbutton");

            // Basic interactivity
            focusable = true;
            pickingMode = PickingMode.Position;

            // Pointer click
            this.RegisterCallback<ClickEvent>(_ => Clicked?.Invoke(Route));
            // Keyboard: Space/Enter activate
            this.RegisterCallback<KeyDownEvent>(e =>
            {
                if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.Space)
                {
                    Clicked?.Invoke(Route);
                    e.StopPropagation();
                }
            });
        }
    }
}
