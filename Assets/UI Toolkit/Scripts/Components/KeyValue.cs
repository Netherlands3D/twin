using Netherlands3D.UI_Toolkit;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class KeyValue : VisualElement
    {
        // Query and cache label component
        private Label keyField;
        private Label KeyField => keyField ??= this.Q<Label>("Key");
        private Label valueField;
        private Label ValueField => valueField ??= this.Q<Label>("Value");

        [UxmlAttribute("key")]
        public string Key
        {
            get => KeyField.text;
            set => KeyField.text = value;
        }
        
        [UxmlAttribute("value")]
        public string Value
        {
            get => ValueField.text;
            set => ValueField.text = value;
        }

        public KeyValue()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");
        }

        public void ShowDivider(bool show)
        {
            EnableInClassList(UtilityClassConstants.HIDDEN, !show);   
        }
    }
}