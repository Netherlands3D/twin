using Netherlands3D.Services;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.UI_Toolkit;
using Netherlands3D.UI.Components;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

namespace Netherlands3D.UI.Panels
{
    [UxmlElement]
    public partial class PropertiesPanel : VisualElement
    {
        private Header header;
        private Button propertiesHeaderCloseButton;
        private ToolbarProperties toolbarProperties;
        private VisualElement informationContent;
        private VisualElement settingsContent;
        private VisualElement stylingContent;

        /// <summary>
        /// Header text pass-through so it can be set from UXML/Properties.
        /// </summary>
        [UxmlAttribute("header-text")]
        public string HeaderText
        {
            get => header?.LabelText;
            set => header.LabelText = value;
        }

        private ToolbarProperties toolbar;
        public ToolbarProperties Toolbar => toolbar ??= this.Q<ToolbarProperties>();

        public VisualElement Content => this.Q("Content");

        public PropertiesPanel()
        {
            this.CloneComponentTree("Panels");
            this.AddComponentStylesheet("Panels");
            
            propertiesHeaderCloseButton = this.Q<CloseButton>();
            header = this.Q<Header>(className: "properties-header-title");
            toolbarProperties = this.Q<ToolbarProperties>();
            informationContent = this.Q<VisualElement>("InformationContent");
            settingsContent = this.Q<VisualElement>("SettingsContent");
            stylingContent = this.Q<VisualElement>("StylingContent");
            
            toolbarProperties.Information.RegisterCallback<ClickEvent>(OnInformationButtonClicked);
            toolbarProperties.Properties.RegisterCallback<ClickEvent>(OnPropertiesButtonClicked);
            toolbarProperties.Styles.RegisterCallback<ClickEvent>(OnStylesButtonClicked);
            propertiesHeaderCloseButton.RegisterCallback<ClickEvent>(OnCloseButtonClick);
            
            OnPropertiesButtonClicked(null);
        }

        private void OnCloseButtonClick(ClickEvent evt)
        {
            var properties = ServiceLocator.GetService<Netherlands3D.Twin.Layers.Properties.Properties>(); //todo: the properties class will be deleted once the Layer inspector panel is implemented
            properties.Hide();
        }
        
        private void OnInformationButtonClicked(ClickEvent evt)
        {
            informationContent.EnableInClassList(UtilityClassConstants.HIDDEN, false);
            settingsContent.EnableInClassList(UtilityClassConstants.HIDDEN, true);
            stylingContent.EnableInClassList(UtilityClassConstants.HIDDEN, true);
        }
        
        private void OnPropertiesButtonClicked(ClickEvent evt)
        {
            informationContent.EnableInClassList(UtilityClassConstants.HIDDEN, true);
            settingsContent.EnableInClassList(UtilityClassConstants.HIDDEN, false);
            stylingContent.EnableInClassList(UtilityClassConstants.HIDDEN, true);
        }
        
        private void OnStylesButtonClicked(ClickEvent evt)
        {
            informationContent.EnableInClassList(UtilityClassConstants.HIDDEN, true);
            settingsContent.EnableInClassList(UtilityClassConstants.HIDDEN, true);
            stylingContent.EnableInClassList(UtilityClassConstants.HIDDEN, false);
        }

        public void SetVisible(bool visible)
        {
            EnableInClassList(UtilityClassConstants.HIDDEN, !visible);
        }

        public void AddPropertySection(VisualElement propertySection, PropertySectionCategory category)
        {
            switch (category)
            {
                case PropertySectionCategory.Information:
                    informationContent.Add(propertySection);
                    break;
                case PropertySectionCategory.Settings:
                    settingsContent.Add(propertySection);
                    break;
                case PropertySectionCategory.Styling:
                    stylingContent.Add(propertySection);
                    break;
                
            }
        }

        public void ClearPropertySections()
        {
            informationContent.Clear();
            settingsContent.Clear();
            stylingContent.Clear();
        }
    }
}
