using System;
using Netherlands3D.Twin;
using Netherlands3D.Twin.PresentationModus.UIHider;
using Netherlands3D.UI.Components;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI_Toolkit.Scripts.Behaviours
{
    public sealed class ToolboxPanelHider : IPanelHider, IDisposable
    {
        private const string PresentationClassName = "toolbar-toolbox--presentation";
        private const float RevealDistance = 16f;
        private const float HoverBuffer = 12f;

        private readonly UIHider uiHider;
        private readonly ToolbarToolbox toolbox;
        private readonly ToolbarScenario scenario;
        private readonly PinToggle pin;

        private bool presentationEnabled;
        private bool registered;
        private bool disposed;

        public bool IsHidden { get; private set; }
        public bool Pinned => pin.value;

        public ToolboxPanelHider(UIHider uiHider, ToolbarToolbox toolbox, ToolbarScenario scenario)
        {
            this.uiHider = uiHider;
            this.toolbox = toolbox;
            this.scenario = scenario;

            pin = toolbox.Q<PinToggle>("PresentationPin");

            pin.RegisterValueChangedCallback(OnPinChanged);
            toolbox.RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            toolbox.RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
            toolbox.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            scenario.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);

            if (toolbox.panel != null)
                Register();
        }

        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            if (evt.target == toolbox)
                Register();
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            if (evt.target == toolbox)
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
            toolbox.EnableInClassList(PresentationClassName, hideUI);
        }

        public void Show()
        {
            IsHidden = false;
            ApplyPosition();
        }

        public void Hide()
        {
            IsHidden = presentationEnabled && !Pinned;
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

        private bool HasVisibleHierarchy()
        {
            if (toolbox.panel == null || !toolbox.visible)
                return false;

            for (var element = (VisualElement)toolbox; element != null; element = element.parent)
            {
                if (element.resolvedStyle.display == DisplayStyle.None)
                    return false;
            }

            return true;
        }

        private Rect GetShownBounds()
        {
            var position = toolbox.parent.LocalToWorld(toolbox.layout.position);
            return new Rect(position, toolbox.layout.size);
        }

        public bool IsMouseOver(Vector2 mousePos)
        {
            if (!HasVisibleHierarchy())
                return false;

            var position = App.UIRoot.GetUIPositionFromScreenPosition(mousePos);
            var topLeft = App.UIRoot.GetUIPositionFromScreenPosition(new Vector2(0f, Screen.height));
            var bottomRight = App.UIRoot.GetUIPositionFromScreenPosition(new Vector2(Screen.width, 0f));

            if (position.x < topLeft.x || position.x > bottomRight.x || position.y < topLeft.y || position.y > bottomRight.y)
                return false;

            if (position.y <= topLeft.y + RevealDistance)
                return true;

            if (IsHidden)
                return false;

            var shownBounds = GetShownBounds();
            var hoverBounds = Rect.MinMaxRect(shownBounds.xMin - HoverBuffer, topLeft.y, shownBounds.xMax + HoverBuffer, shownBounds.yMax + HoverBuffer);

            return hoverBounds.Contains(position);
        }

        private void ApplyPosition()
        {
            if (toolbox.panel == null || toolbox.parent == null)
                return;

            if (!IsHidden)
            {
                toolbox.style.translate = new Translate(0f, 0f);
                scenario.style.translate = new Translate(0f, 0f);
                return;
            }

            var shownBounds = GetShownBounds();

            if (float.IsNaN(shownBounds.height) || shownBounds.height <= 0f)
                return;

            var screenTop = App.UIRoot.GetUIPositionFromScreenPosition(new Vector2(0f, Screen.height)).y;
            toolbox.style.translate = new Translate(0f, screenTop - shownBounds.yMax - 1f);

            if (scenario.resolvedStyle.display == DisplayStyle.None)
                return;

            var scenarioOffset = toolbox.layout.y - scenario.layout.y;
            scenario.style.translate = new Translate(0f, scenarioOffset);
        }

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;
            Unregister();

            pin.UnregisterValueChangedCallback(OnPinChanged);
            toolbox.UnregisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            toolbox.UnregisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
            toolbox.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            scenario.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);

            toolbox.RemoveFromClassList(PresentationClassName);
            toolbox.style.translate = StyleKeyword.Null;
            scenario.style.translate = StyleKeyword.Null;
        }
    }
}