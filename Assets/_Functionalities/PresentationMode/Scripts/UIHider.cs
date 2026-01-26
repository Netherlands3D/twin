using Netherlands3D.Services;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace Netherlands3D.Twin.PresentationModus.UIHider
{
    public enum UIExitDirection {  Left, Right, Up, Down }

    public class UIHider : MonoBehaviour
    {
        [SerializeField] private InputActionReference hideButton;

        private List<IPanelHider> panelHiders = new List<IPanelHider>();
        private bool hideUI;

        [Header("Snackbar")]
        [SerializeField] private UnityEvent showHideText;

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
            if (!panelHiders.Contains(panelHider)) panelHiders.Add(panelHider);
            SetPanelHide(panelHider);
        }

        public void Unregister(IPanelHider panelHider)
        {
            if (panelHiders.Contains(panelHider)) panelHiders.Remove(panelHider);
        }

        private void Update()
        {
            Vector2 mousePosition = Pointer.current.position.ReadValue();

            if (!hideUI) return;

            panelHiders.ForEach(panel =>
            {
                bool isMouseOver = panel.IsMouseOver(mousePosition);

                if (isMouseOver && panel.IsHidden) panel.Show();
                else if(!isMouseOver && !panel.Pinned && !panel.IsHidden) panel.Hide();
            });
        }

        public void ToggleUIHider()
        {
            hideUI = !hideUI;

            panelHiders.ForEach(panel => SetPanelHide(panel));
            if (hideUI) showHideText.Invoke();
        }

        private void SetPanelHide(IPanelHider panel)
        {
            panel.HideUI(hideUI);
            if (hideUI && !panel.Pinned) panel.Hide();
            else if (!hideUI) panel.Show();
        }

        private void OnHideUIPressed(InputAction.CallbackContext context)
        {
            //TODO: Switch this out for a inputfield checker instead of using the one from the FPV.
            if (ServiceLocator.GetService<FirstPersonViewer.FirstPersonViewer>().Input.IsInputfieldSelected()) return;

            ToggleUIHider();
        }
    }
}
