using GltfMeshFeatures;
using Netherlands3D.Services;
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
        private VisualElement propertiesContent;
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
            propertiesContent = this.Q<VisualElement>("PropertiesContent");
            stylingContent = this.Q<VisualElement>("StylingContent");
            
            toolbarProperties.Information.RegisterCallback<ClickEvent>(OnInformationButtonClicked);
            toolbarProperties.Properties.RegisterCallback<ClickEvent>(OnPropertiesButtonClicked);
            toolbarProperties.Styles.RegisterCallback<ClickEvent>(OnStylesButtonClicked);
            propertiesHeaderCloseButton.RegisterCallback<ClickEvent>(OnCloseButtonClick);
            
        }

        private void OnCloseButtonClick(ClickEvent evt)
        {
            var properties = ServiceLocator.GetService<Netherlands3D.Twin.Layers.Properties.Properties>(); //todo: the properties class will be deleted once the Layer inspector panel is implemented
            properties.Hide();
        }
        
        private void OnInformationButtonClicked(ClickEvent evt)
        {
            informationContent.EnableInClassList(UtilityClassConstants.HIDDEN, false);
            propertiesContent.EnableInClassList(UtilityClassConstants.HIDDEN, true);
            stylingContent.EnableInClassList(UtilityClassConstants.HIDDEN, true);
        }
        
        private void OnPropertiesButtonClicked(ClickEvent evt)
        {
            informationContent.EnableInClassList(UtilityClassConstants.HIDDEN, true);
            propertiesContent.EnableInClassList(UtilityClassConstants.HIDDEN, false);
            stylingContent.EnableInClassList(UtilityClassConstants.HIDDEN, true);
        }
        
        private void OnStylesButtonClicked(ClickEvent evt)
        {
            informationContent.EnableInClassList(UtilityClassConstants.HIDDEN, true);
            propertiesContent.EnableInClassList(UtilityClassConstants.HIDDEN, true);
            stylingContent.EnableInClassList(UtilityClassConstants.HIDDEN, false);
        }

        public void SetVisible(bool visible)
        {
            EnableInClassList(UtilityClassConstants.HIDDEN, !visible);
        }

        public void AddPropertySection(VisualElement propertySection)
        {
            informationContent.Add(propertySection);
        }

        public void ClearPropertySections()
        {
            informationContent.Clear();
        }
    }
}
