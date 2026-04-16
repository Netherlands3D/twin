using Netherlands3D.UI_Toolkit.Scripts;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class ColorTile : VisualElement
    {
        private VisualElement fill;
        private VisualElement Fill => fill ??= this.Q<VisualElement>("Fill");

        private Label labelField;
        private Label Label => labelField ??= this.Q<Label>("Label");

        private bool showLabel;
        [UxmlAttribute("show-label")]
        public bool ShowLabel
        {
            get => showLabel;
            set
            {
                showLabel = value;
                UpdateLabelVisibility();
            }
        }

        [UxmlAttribute("label-text")]
        public string LabelText
        {
            get => Label?.text;
            set
            {
                if (Label != null)
                    Label.text = value;
            }
        }

        private string colorHex = "#FFC142";
        [UxmlAttribute("color")]
        public string Color
        {
            get => colorHex;
            set
            {
                colorHex = value;
                ApplyColor();
            }
        }

        public ColorTile()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");

            AddToClassList("color-tile");

            RegisterCallback<AttachToPanelEvent>(_ =>
            {
                UpdateLabelVisibility();
                ApplyColor();
            });
        }

        private void UpdateLabelVisibility()
        {
            if (Label == null)
                return;

            Label.style.display = showLabel ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void ApplyColor()
        {
            if (Fill == null)
                return;

            if (HexColorUtility.ParseHexColor(colorHex, out var parsedColor))
            {
                Fill.style.backgroundColor = parsedColor;
            }
        }
    }
}