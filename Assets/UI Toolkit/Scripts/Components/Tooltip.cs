using Netherlands3D.UI.ExtensionMethods;
using Netherlands3D.UI_Toolkit.Scripts;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class Tooltip : VisualElement
    {
        private Label labelField;
        private Label Label => labelField ??= this.Q<Label>("Label");

        [UxmlAttribute("text")]
        public string Text
        {
            get => Label?.text;
            set
            {
                if (Label != null)
                    Label.text = value;
            }
        }

        public Tooltip()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");

            AddToClassList("tooltip");
        }

        public void Show()
        {
            EnableInClassList("active", true);
        }

        public void Hide()
        {
            EnableInClassList("active", false);
        }
    }
}