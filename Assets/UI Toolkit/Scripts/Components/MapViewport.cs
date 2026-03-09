using Netherlands3D.UI.ExtensionMethods;
using Netherlands3D.UI_Toolkit.Scripts;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class MapViewport : VisualElement
    {
        private VisualElement mapLayer;
        private VisualElement overlayLayer;
        private Icon locationPin;

        public VisualElement MapLayer => mapLayer ??= this.Q<VisualElement>("MapLayer");
        public VisualElement OverlayLayer => overlayLayer ??= this.Q<VisualElement>("OverlayLayer");
        public Icon LocationPin => locationPin ??= this.Q<Icon>("LocationPin");

        private bool showPin = true;

        [UxmlAttribute("show-pin")]
        public bool ShowPin
        {
            get => showPin;
            set
            {
                showPin = value;
                EnableInClassList("show-pin", value);
                if (LocationPin != null) LocationPin.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        public MapViewport()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");

            AddToClassList("map-viewport");

            RegisterCallback<AttachToPanelEvent>(_ =>
            {
                // Ensure pin visibility matches attribute
                if (LocationPin != null)
                    LocationPin.style.display = showPin ? DisplayStyle.Flex : DisplayStyle.None;
            });

            // TODO: Implement clickable map viewport rendering.
            // This element should later display a map rendered (e.g. via a RenderTexture bridge or a custom tile renderer).
            // Pointer input (click/drag/scroll) should be forwarded to the map/navigation logic to place/update the pin and update coordinates.
        }
    }
}
