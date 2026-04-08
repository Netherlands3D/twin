using UnityEngine;

namespace Netherlands3D.Twin.PresentationModus.UIHider
{
    public interface IPanelHider
    {
        public bool IsHidden { get; }
        public bool Pinned { get; }

        public bool IsMouseOver(Vector2 mousePos);

        public void HideUI(bool hideUI);
        public void Show();
        public void Hide();
    }
}
