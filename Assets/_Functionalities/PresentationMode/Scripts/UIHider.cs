using Netherlands3D.Twin;
using Netherlands3D.Services;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Netherlands3D.Twin.PresentationModus.UIHider
{
    public enum UIExitDirection { Left, Right, Up, Down }

    public class UIHider : MonoBehaviour
    {
        [SerializeField] private InputActionReference hideButton;

        private readonly List<IPanelHider> panelHiders = new();
        private bool hideUI;
        private const float RevealDelay = 0.15f;
        private const float HideDelay = 0.30f;
        private const float MinimumShowTime = 0.40f;

        private readonly Dictionary<IPanelHider, HoverTiming> hoverTimings = new();

        public const string FunctionalityId = "presentation-mode";

        public bool IsPresenting { get; private set; }
        public event System.Action PresentationChanged;

        private void Start()
        {
            hideButton.action.performed += OnHideUIPressed;
        }

        private void OnDestroy()
        {
            hideButton.action.performed -= OnHideUIPressed;
        }

        public void Register(IPanelHider panelHider)
        {
            if (!panelHiders.Contains(panelHider))
                panelHiders.Add(panelHider);

            SetPanelHide(panelHider);
        }

        public void Unregister(IPanelHider panelHider)
        {
            panelHiders.Remove(panelHider);
            hoverTimings.Remove(panelHider);
        }

        private void Update()
        {
            if (!hideUI || IsPresenting || Pointer.current == null)
                return;

            var mousePosition = Pointer.current.position.ReadValue();
            var now = Time.unscaledTime;

            for (var i = 0; i < panelHiders.Count; i++)
            {
                var panel = panelHiders[i];
                var timing = hoverTimings[panel];

                if (timing.IsHidden != panel.IsHidden)
                {
                    timing.IsHidden = panel.IsHidden;
                    timing.StartedAt = -1f;
                }

                if (panel.Pinned)
                {
                    timing.StartedAt = -1f;
                    hoverTimings[panel] = timing;
                    continue;
                }

                var isMouseOver = panel.IsMouseOver(mousePosition);
                var shouldChangeVisibility = panel.IsHidden ? isMouseOver : !isMouseOver;

                if (!shouldChangeVisibility)
                {
                    timing.StartedAt = -1f;
                }
                else if (timing.StartedAt < 0f)
                {
                    timing.StartedAt = now;
                }
                else
                {
                    var delay = panel.IsHidden ? RevealDelay : HideDelay;

                    if (now - timing.StartedAt >= delay && (panel.IsHidden || now >= timing.EarliestHideAt))
                    {
                        if (panel.IsHidden)
                        {
                            panel.Show();
                            timing.EarliestHideAt = now + MinimumShowTime;
                        }
                        else
                        {
                            panel.Hide();
                        }

                        timing.IsHidden = panel.IsHidden;
                        timing.StartedAt = -1f;
                    }
                }

                hoverTimings[panel] = timing;
            }
        }

        public void ToggleUIHider()
        {
            SetPresenting(!IsPresenting);
        }

        public void SetPresenting(bool presenting)
        {
            presenting &= hideUI;

            if (IsPresenting == presenting)
                return;

            IsPresenting = presenting;
            RefreshPanels();
            PresentationChanged?.Invoke();
        }

        public void SetPresentationEnabled(bool enabled)
        {
            hideUI = enabled;

            if (!enabled && IsPresenting)
            {
                SetPresenting(false);
                return;
            }

            RefreshPanels();
        }

        public void RefreshPanels()
        {
            foreach (var panel in panelHiders)
                SetPanelHide(panel);
        }

        private void SetPanelHide(IPanelHider panel)
        {
            panel.HideUI(hideUI);

            if (hideUI && !panel.Pinned)
                panel.Hide();
            else
                panel.Show();

            hoverTimings[panel] = new HoverTiming
            {
                IsHidden = panel.IsHidden,
                StartedAt = -1f
            };
        }

        private void OnHideUIPressed(InputAction.CallbackContext context)
        {
            //TODO: Switch this out for a inputfield checker instead of using the one from the FPV.
            if (ServiceLocator.GetService<FirstPersonViewer.FirstPersonViewer>().Input.IsInputfieldSelected()) 
                return;

            ToggleUIHider();
        }

        private struct HoverTiming
        {
            public bool IsHidden;
            public float StartedAt;
            public float EarliestHideAt;
        }
    }
}
