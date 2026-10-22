using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class CheckboxToggle : UnityEngine.UIElements.Toggle
    {
        public enum CheckboxPosition
        {
            Left,
            Right
        }

        // Query and cache label component
        private Label labelField;
        private Label Label => labelField ??= this.Q<Label>("Label");

        private CheckboxPosition checkboxPosition = CheckboxPosition.Left;
        [UxmlAttribute("checkbox-position")]
        public CheckboxPosition boxPosition
        {
            get => checkboxPosition;
            set { checkboxPosition = value; UpdateClassList(); }
        }

        [UxmlAttribute("LabelText")]
        public string LabelText
        {
            get => Label.text;
            set => Label.text = value;
        }

        public CheckboxToggle()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");

            RegisterCallback<AttachToPanelEvent>(_ => UpdateClassList());
        }

        private void UpdateClassList()
        {
            this.ReplacePrefixedValueInClassList("checkbox-position-", checkboxPosition.ToString().ToKebabCase());
        }
    }
}