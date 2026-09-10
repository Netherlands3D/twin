using System;
using System.Collections.Generic;
using Netherlands3D.Twin;
using Netherlands3D.Twin.PresentationModus.UIHider;
using Netherlands3D.UI.Components;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI_Toolkit.Scripts.Behaviours
{
    public sealed class PresentationPanelHider : IPanelHider, IDisposable
    {
        private const string PresentationClassName = "presentation-section--active";
        private const string HiddenClassName = "presentation-section--hidden";
        private const float RevealDistance = 16f;
        private const float BottomRevealDistance = 80f;
        private const float HoverBuffer = 12f;

        private readonly UIHider uiHider;
        private readonly VisualElement section;
        private readonly VisualElement root;
        private readonly PinToggle pin;
        private readonly UIExitDirection direction;
        private readonly List<VisualElement> hoverAreas;
        private readonly bool releaseHorizontalSpace;
        private bool horizontalSpaceReleased;
        private readonly VisualElement hoverExclusion;

        private bool presentationEnabled;
        private bool registered;
        private bool disposed;

        public bool IsHidden { get; private set; }
        public bool Pinned => pin.value;

        public PresentationPanelHider(UIHider uiHider, VisualElement section, PinToggle pin, UIExitDirection direction, bool releaseHorizontalSpace = false, VisualElement hoverExclusion = null)
        {
            this.uiHider = uiHider;
            this.section = section;
            this.pin = pin;
            this.direction = direction;
            this.releaseHorizontalSpace = releaseHorizontalSpace;
            this.hoverExclusion = hoverExclusion;

            root = App.UIRoot.Root;
            hoverAreas = section.Query<VisualElement>(className: "presentation-section__hover-area").ToList();

            pin.RegisterValueChangedCallback(OnPinChanged);
            section.RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            section.RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
            section.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            section.RegisterCallback<TransitionEndEvent>(OnTransitionEnd);
            root.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);

            if (section.panel != null)
                Register();
        }

        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            if (evt.target == section)
                Register();
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            if (evt.target == section)
                Unregister();
        }

        private void Register()
        {
            if (registered || disposed)
                return;

            registered = true;
            uiHider.Register(this);
        }

        private void Unregister()
        {
            if (!registered)
                return;

            registered = false;
            uiHider.Unregister(this);
        }

        public void HideUI(bool hideUI)
        {
            presentationEnabled = hideUI;
            section.EnableInClassList(PresentationClassName, hideUI);
        }

        public void Show()
        {
            IsHidden = false;
            horizontalSpaceReleased = false;
            section.RemoveFromClassList(HiddenClassName);
            ApplyPosition();
        }

        public void Hide()
        {
            if (!presentationEnabled || Pinned)
            {
                Show();
                return;
            }

            if (IsHidden)
                return;

            IsHidden = true;
            section.AddToClassList(HiddenClassName);
            ApplyPosition();
        }

        private void OnPinChanged(ChangeEvent<bool> evt)
        {
            if (evt.newValue)
                Show();
            else
                Hide();
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            ApplyPosition();
        }

        private void OnTransitionEnd(TransitionEndEvent evt)
        {
            if (disposed || evt.target != section || !releaseHorizontalSpace || !IsHidden || horizontalSpaceReleased)
                return;

            if (!evt.stylePropertyNames.Contains("translate"))
                return;

            horizontalSpaceReleased = true;
            ApplyPosition();
        }

        private bool HasVisibleHierarchy()
        {
            if (section.panel == null || section.parent == null || !section.visible)
                return false;

            for (var element = section; element != null; element = element.parent)
            {
                if (element.resolvedStyle.display == DisplayStyle.None)
                    return false;
            }

            return true;
        }

        private Rect GetShownBounds()
        {
            return section.parent.LocalToWorld(section.layout);
        }

        private Rect GetScreenBounds()
        {
            var topLeft = App.UIRoot.GetUIPositionFromScreenPosition(new Vector2(0f, Screen.height));
            var bottomRight = App.UIRoot.GetUIPositionFromScreenPosition(new Vector2(Screen.width, 0f));

            return Rect.MinMaxRect(topLeft.x, topLeft.y, bottomRight.x, bottomRight.y);
        }

        public bool IsMouseOver(Vector2 mousePos)
        {
            if (!HasVisibleHierarchy())
                return false;

            var position = App.UIRoot.GetUIPositionFromScreenPosition(mousePos);
            var screenBounds = GetScreenBounds();

            if (!ContainsPosition(screenBounds, position))
                return false;

            if (hoverExclusion != null && hoverExclusion.panel == section.panel && hoverExclusion.visible && hoverExclusion.resolvedStyle.display != DisplayStyle.None)
            {
                var bounds = hoverExclusion.worldBound;
                bounds.xMin -= HoverBuffer;
                bounds.xMax += HoverBuffer;
                bounds.yMin -= HoverBuffer;
                bounds.yMax += HoverBuffer;

                if (ContainsPosition(bounds, position))
                    return !IsHidden;
            }

            if (ContainsPosition(GetRevealBounds(screenBounds), position))
                return true;

            if (IsHidden)
                return false;

            for (var i = 0; i < hoverAreas.Count; i++)
            {
                var area = hoverAreas[i];

                if (!IsHoverAreaVisible(area))
                    continue;

                var bounds = area.worldBound;

                bounds.xMin -= HoverBuffer;
                bounds.xMax += HoverBuffer;
                bounds.yMin -= HoverBuffer;
                bounds.yMax += HoverBuffer;

                if (ContainsPosition(bounds, position))
                    return true;
            }

            return false;
        }

        private bool IsHoverAreaVisible(VisualElement area)
        {
            if (area.panel != section.panel || !area.visible || !section.Contains(area))
                return false;

            for (var element = area; element != section; element = element.parent)
            {
                if (element.resolvedStyle.display == DisplayStyle.None)
                    return false;
            }

            var bounds = area.worldBound;
            return bounds.width > 0f && bounds.height > 0f;
        }

        private Rect GetRevealBounds(Rect screenBounds)
        {
            var bounds = screenBounds;

            switch (direction)
            {
                case UIExitDirection.Left:
                    bounds.xMax = bounds.xMin + RevealDistance;
                    break;

                case UIExitDirection.Right:
                    bounds.xMin = bounds.xMax - RevealDistance;
                    break;

                case UIExitDirection.Up:
                    bounds.yMax = bounds.yMin + RevealDistance;
                    break;

                case UIExitDirection.Down:
                    bounds.yMin = bounds.yMax - BottomRevealDistance;
                    break;
            }

            return bounds;
        }

        private static bool ContainsPosition(Rect bounds, Vector2 position)
        {
            return position.x >= bounds.xMin && position.x <= bounds.xMax &&
                   position.y >= bounds.yMin && position.y <= bounds.yMax;
        }

        private void ApplyPosition()
        {
            if (section.panel == null || section.parent == null)
                return;

            if (releaseHorizontalSpace)
            {
                var width = section.layout.width;

                if (!float.IsNaN(width) && width > 0f)
                    section.style.marginRight = IsHidden && horizontalSpaceReleased ? -width : 0f;
            }

            if (!IsHidden)
            {
                section.style.translate = new Translate(0f, 0f);
                return;
            }

            if (!HasVisibleHierarchy())
                return;

            var bounds = GetShownBounds();

            if (float.IsNaN(bounds.width) || float.IsNaN(bounds.height) || bounds.width <= 0f || bounds.height <= 0f)
                return;

            var screenBounds = GetScreenBounds();
            var offset = direction switch
            {
                UIExitDirection.Left => new Vector2(screenBounds.xMin - bounds.xMax - 1f, 0f),
                UIExitDirection.Right => new Vector2(screenBounds.xMax - bounds.xMin + 1f, 0f),
                UIExitDirection.Up => new Vector2(0f, screenBounds.yMin - bounds.yMax - 1f),
                UIExitDirection.Down => new Vector2(0f, screenBounds.yMax - bounds.yMin + 1f),
                _ => Vector2.zero
            };

            var origin = section.parent.WorldToLocal(bounds.position);
            var destination = section.parent.WorldToLocal(bounds.position + offset);
            var localOffset = destination - origin;

            section.style.translate = new Translate(localOffset.x, localOffset.y);
        }

        public void Dispose()
        {
            if (disposed)
                return;

            section.RemoveFromClassList(HiddenClassName);

            if (releaseHorizontalSpace)
                section.style.marginRight = StyleKeyword.Null;

            IsHidden = false;
            disposed = true;
            Unregister();

            pin.UnregisterValueChangedCallback(OnPinChanged);
            section.UnregisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            section.UnregisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
            section.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            section.UnregisterCallback<TransitionEndEvent>(OnTransitionEnd);
            root.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);

            section.RemoveFromClassList(PresentationClassName);
            section.style.translate = StyleKeyword.Null;
        }
    }
}