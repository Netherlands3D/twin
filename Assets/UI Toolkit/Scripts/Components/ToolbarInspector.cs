using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class ToolbarInspector : VisualElement
    {
        // TODO: clean-up OpenLibrary and AddToLibrary after removing buttons 
        // also clean-up corresponding behaviours in InspectorPanelBehaviour.cs
        public Toggle OpenLibrary => this.Q<Toggle>("OpenLibrary");
        private Button AddFolder => this.Q<Button>("AddFolder");
        private Button Delete => this.Q<Button>("Delete");
        private Toggle AddToLibrary => this.Q<Toggle>("AddToLibrary");
        public Toggle AddLayer => this.Q<Toggle>("AddLayer");

        public EventCallback<ChangeEvent<bool>> OnOpenLibraryToggled { get; set; }
        public EventCallback<ClickEvent> OnAddFolderClicked { get; set; }
        public EventCallback<ClickEvent> OnDeleteClicked { get; set; }
        public EventCallback<ChangeEvent<bool>> OnAddToLibraryToggled { get; set; }
        public EventCallback<ChangeEvent<bool>> OnAddLayerToggled { get; set; }
        
        public enum ToolbarStyle 
        { 
            Normal, 
            Library, 
            AddLayer 
        }

        private ToolbarStyle toolbarStyle = ToolbarStyle.Normal;

        [UxmlAttribute("toolbar-style")]
        public ToolbarStyle Style
        {
            get => toolbarStyle;
            set { toolbarStyle = value; UpdateClassList(); }
        }

        public ToolbarInspector()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");

            // Register exposed events
            OpenLibrary.RegisterValueChangedCallback(evt => OnOpenLibraryToggled?.Invoke(evt));
            AddFolder.RegisterCallback<ClickEvent>(evt => OnAddFolderClicked?.Invoke(evt));
            Delete.RegisterCallback<ClickEvent>(evt => OnDeleteClicked?.Invoke(evt));
            AddToLibrary.RegisterValueChangedCallback(evt => OnAddToLibraryToggled?.Invoke(evt));
            AddLayer.RegisterValueChangedCallback(evt => OnAddLayerToggled?.Invoke(evt));
            
            // Ensure initial style is correctly set
            RegisterCallback<AttachToPanelEvent>(_ => UpdateClassList());
        }
        
        private void UpdateClassList()
        {
            this.ReplacePrefixedValueInClassList("toolbar-style-", toolbarStyle.ToString().ToKebabCase());
        }
    }
}
