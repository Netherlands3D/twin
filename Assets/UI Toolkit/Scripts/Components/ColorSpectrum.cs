using Netherlands3D.UI.ExtensionMethods;
using Netherlands3D.UI_Toolkit.Scripts;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class ColorSpectrum : VisualElement
    {
        private VisualElement thumb;
        private VisualElement Thumb => thumb ??= this.Q<VisualElement>("Thumb");

        private float selectorX = 80f;
        [UxmlAttribute("selector-x")]
        public float SelectorX
        {
            get => selectorX;
            set
            {
                selectorX = value;
                ApplySelectorPosition();
            }
        }

        private float selectorY = 80f;
        [UxmlAttribute("selector-y")]
        public float SelectorY
        {
            get => selectorY;
            set
            {
                selectorY = value;
                ApplySelectorPosition();
            }
        }

        public ColorSpectrum()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");

            AddToClassList("color-spectrum");

            RegisterCallback<AttachToPanelEvent>(_ => ApplySelectorPosition());
        }

        private void ApplySelectorPosition()
        {
            if (Thumb == null)
                return;

            Thumb.style.left = selectorX;
            Thumb.style.top = selectorY;
        }
    }
}