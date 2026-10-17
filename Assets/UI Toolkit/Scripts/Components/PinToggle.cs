using Netherlands3D.UI.ExtensionMethods;
using Netherlands3D.UI_Toolkit.Scripts;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    // TODO: Extract a reusable IconToggle with configurable on/off icons when another feature needs it.
    [UxmlElement]
    public partial class PinToggle : UnityEngine.UIElements.Toggle
    {
        private Icon icon;

        public override bool value
        {
            get => base.value;
            set
            {
                base.value = value;
                UpdateIcon();
            }
        }

        public PinToggle()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");
            AddToClassList("pin-toggle");

            icon = this.Q<Icon>("Icon");
            UpdateIcon();
        }

        public override void SetValueWithoutNotify(bool newValue)
        {
            base.SetValueWithoutNotify(newValue);
            UpdateIcon();
        }

        private void UpdateIcon()
        {
            if (icon == null)
                return;

            var image = value ? IconImage.PINNED : IconImage.UNPINNED;

            if (icon.Image != image)
                icon.Image = image;

            tooltip = value ? "Sectie losmaken" : "Sectie vastzetten";
        }
    }
}