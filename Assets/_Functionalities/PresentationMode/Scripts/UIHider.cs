using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Netherlands3D
{
    public enum UIExitDirection {  Left, Right, Up, Down }

    public class UIHider : MonoBehaviour
    {
        private List<IPanelHider> panelHiders = new List<IPanelHider>();
        private bool hideUI;

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
            if (Input.GetKeyDown(KeyCode.L)) ToggleUIHider();
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
        }

        private void SetPanelHide(IPanelHider panel)
        {
            panel.HideUI(hideUI);
            if (hideUI && !panel.Pinned) panel.Hide();
            else if (!hideUI) panel.Show();
        }
    }
}
