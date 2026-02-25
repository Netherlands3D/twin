using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class PropertiesPanel : VisualElement
    {
        private Label header;
        private Label Header => header ??= this.Q<Label>(className: "properties-header-title");
        private Button propertiesHeaderCloseButton;
        public Button PropertiesHeaderCloseButton => propertiesHeaderCloseButton ??= this.Q<Button>("PropertiesHeaderCloseButton");

        /// <summary>
        /// Header text pass-through so it can be set from UXML/Properties.
        /// </summary>
        [UxmlAttribute("header-text")]
        public string HeaderText
        {
            get => Header?.text;
            set { if (Header != null) Header.text = value; }
        }

        private ToolbarProperties toolbar;
        public ToolbarProperties Toolbar => toolbar ??= this.Q<ToolbarProperties>();

        public VisualElement Content => this.Q("Content");

        public PropertiesPanel()
        {
            this.CloneComponentTree("Panels");
            this.AddComponentStylesheet("Panels");
        }

        public void Open()
        {
            EnableInClassList("active", true);
        }

        public void Close()
        {
            EnableInClassList("active", false);
        }
    }
}
