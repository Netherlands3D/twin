using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class ToolbarInspector : VisualElement
    {
        public Toggle OpenLibrary => this.Q<Toggle>("OpenLibrary");
        private Button AddFolder => this.Q<Button>("AddFolder");
        private Button Delete => this.Q<Button>("Delete");
        private Toggle AddToLibrary => this.Q<Toggle>("AddToLibrary");
        private Toggle AddLayer => this.Q<Toggle>("AddLayer");

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
            set
            {
                toolbarStyle = value;
                ApplyToolbarStyle();
            }
        }

        public ToolbarInspector()
        {
            // Find and load UXML template for this component
            var asset = Resources.Load<VisualTreeAsset>("UI/" + nameof(ToolbarInspector));
            asset.CloneTree(this);

            // Find and load USS stylesheet specific for this component
            var styleSheet = Resources.Load<StyleSheet>("UI/" + nameof(ToolbarInspector) + "-style");
            styleSheets.Add(styleSheet);

            // Register exposed events
            OpenLibrary.RegisterValueChangedCallback(evt => OnOpenLibraryToggled?.Invoke(evt));
            AddFolder.RegisterCallback<ClickEvent>(evt => OnAddFolderClicked?.Invoke(evt));
            Delete.RegisterCallback<ClickEvent>(evt => OnDeleteClicked?.Invoke(evt));
            AddToLibrary.RegisterValueChangedCallback(evt => OnAddToLibraryToggled?.Invoke(evt));
            AddLayer.RegisterValueChangedCallback(evt => OnAddLayerToggled?.Invoke(evt));
            
            // Ensure initial style is correctly set
            RegisterCallback<AttachToPanelEvent>(_ => ApplyToolbarStyle());
        }
        
        private void ApplyToolbarStyle()
        {
            EnableInClassList("toolbar-style-normal", toolbarStyle == ToolbarStyle.Normal);
            EnableInClassList("toolbar-style-library", toolbarStyle == ToolbarStyle.Library);
            EnableInClassList("toolbar-style-add-layer", toolbarStyle == ToolbarStyle.AddLayer);
        }
    }
}
