using Netherlands3D.UI_Toolkit;
using Netherlands3D.UI_Toolkit.Scripts;
using Netherlands3D.UI.Components;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

namespace Netherlands3D.UI.Panels
{
    [UxmlElement]
    public partial class SecondaryPropertiesPanel : VisualElement
    {
        private ContentContainer contentContainer;
        
        /// <summary>
        /// Header text pass-through so it can be set from UXML/Properties.
        /// </summary>
        [UxmlAttribute("header-text")]
        public string HeaderText
        {
            get => contentContainer.HeaderText;
            set => contentContainer.HeaderText = value;
        }
        
        [UxmlAttribute("icon")]
        public string Icon
        {
            get => contentContainer.LeadingIconImage;
            set => contentContainer.LeadingIconImage = value;
        }

        public SecondaryPropertiesPanel()
        {
            this.CloneComponentTree("Panels");
            this.AddComponentStylesheet("Panels");
            
            contentContainer = this.Q<ContentContainer>();
            contentContainer.CloseButtonClicked.AddListener(() => SetVisible(false));
            
            this.Q<ColorPicker>().ColorPickerVisibilityChanged.AddListener(SetVisible);
        }

        public void SetVisible(bool visible)
        {
            EnableInClassList(UtilityClassConstants.HIDDEN, !visible);
        }
    }
}
