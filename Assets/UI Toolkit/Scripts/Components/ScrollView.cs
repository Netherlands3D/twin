using Netherlands3D.UI;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    /// <summary>
    /// NL3D ScrollView wrapper around UI Toolkit ScrollView.
    /// - Loads UXML and USS from Resources/UI.
    /// - Exposes common attributes while keeping defaults theme-driven.
    /// - Adds a stable root class "scrollview" for styling.
    /// </summary>
    [UxmlElement]
    public partial class ScrollView : UnityEngine.UIElements.ScrollView
    {
        private float itemGap = 8f;
        private int _lastGapChildCount = -1;

        [UxmlAttribute("mode")]
        public ScrollViewMode Mode
        {
            get => base.mode;
            set => base.mode = value;
        }

        /// <summary>Vertical spacing between direct children in the content container.</summary>
        [UxmlAttribute("item-gap")]
        public float ItemGap
        {
            get => itemGap;
            set
            {
                itemGap = value;
                ApplyItemGap(force: true);
            }
        }

        // Scroller visibility (maps to built-in visibilities). Defaults to Auto.
        [UxmlAttribute("vertical-scroller-visibility")]
        public ScrollerVisibility VerticalVisibility
        {
            get => verticalScrollerVisibility;
            set => verticalScrollerVisibility = value;
        }

        [UxmlAttribute("horizontal-scroller-visibility")]
        public ScrollerVisibility HorizontalVisibility
        {
            get => horizontalScrollerVisibility;
            set => horizontalScrollerVisibility = value;
        }

        // Optional tuning (kept minimal; safe defaults)
        private float decel = 0.135f;
        [UxmlAttribute("scroll-deceleration-rate")]
        public float ScrollDecelerationRate
        {
            get => decel;
            set { decel = value; scrollDecelerationRate = value; }
        }

        private float elasticityValue = 0.1f;
        [UxmlAttribute("elasticity")]
        public float Elasticity
        {
            get => elasticityValue;
            set { elasticityValue = value; elasticity = value; }
        }

        public ScrollView()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");

            RegisterCallback<AttachToPanelEvent>(_ =>
            {
                HookGapHandlers();
                ApplyItemGap(force: true);
            });
        }

        private void ApplyItemGap(bool force = false)
        {
            var cont = contentContainer;
            if (cont == null) return;

            int count = cont.childCount;
            if (!force && count == _lastGapChildCount) return;
            _lastGapChildCount = count;

            float gap = itemGap;
            for (int i = 0; i < count; i++)
            {
                var child = cont.ElementAt(i);
                child.style.marginTop = (i == 0) ? 0 : gap;
            }
        }

        private void HookGapHandlers()
        {
            var cont = contentContainer;
            if (cont == null) return;

            // Re-apply whenever the container's geometry (and thus children/layout) changes.
            cont.RegisterCallback<GeometryChangedEvent>(_ => ApplyItemGap());
        }
    }
}