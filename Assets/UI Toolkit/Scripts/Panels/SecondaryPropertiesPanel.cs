using Netherlands3D.UI_Toolkit;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Panels
{
    [UxmlElement]
    public partial class SecondaryPropertiesPanel : VisualElement
    {
        private Button propertiesHeaderCloseButton;
        public Button PropertiesHeaderCloseButton => propertiesHeaderCloseButton ??= this.Q<Button>("PropertiesHeaderCloseButton");

        // /// <summary>
        // /// Header text pass-through so it can be set from UXML/Properties.
        // /// </summary>
        // [UxmlAttribute("header-text")]
        // public string HeaderText
        // {
        //     get => Header?.LabelText;
        //     set { if (Header != null) Header.LabelText = value; }
        // }

        public SecondaryPropertiesPanel()
        {
            this.CloneComponentTree("Panels");
            this.AddComponentStylesheet("Panels");
        }

        public void SetVisible(bool visible)
        {
            EnableInClassList(UtilityClassConstants.HIDDEN, !visible);
        }
    }
}
