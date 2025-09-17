using Netherlands3D.UI;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class ToolbarInspector : VisualElement
    {
        private Toggle OpenLibrary => this.Q<Toggle>("OpenLibrary");
        private Button AddFolder => this.Q<Button>("AddFolder");
        private Button Delete => this.Q<Button>("Delete");
        private Toggle AddToLibrary => this.Q<Toggle>("AddToLibrary");
        private Toggle AddLayer => this.Q<Toggle>("AddLayer");

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
                ApplyToolbarStyle(); // explicit, like ContentContainer.ApplyContainerType()
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

            this.RegisterCallback<AttachToPanelEvent>(_ =>
            {
                InitializeButtons();
                ApplyToolbarStyle();
            });
        }

        /// <summary>
        /// Ensure exactly one toolbar-style-* class is active on this root.
        /// Mirrors the explicit state-apply approach used in ContentContainer.
        /// </summary>
        private void ApplyToolbarStyle()
        {
            // Turn off all style classes first
            EnableInClassList("toolbar-style-Normal", toolbarStyle == ToolbarStyle.Normal);
            EnableInClassList("toolbar-style-Library", toolbarStyle == ToolbarStyle.Library);
            EnableInClassList("toolbar-style-AddLayer", toolbarStyle == ToolbarStyle.AddLayer);
        }

        private void InitializeButtons()
        {
            if (OpenLibrary != null)
            {
                OpenLibrary.ShowIcon = Toggle.ToggleStyle.IconOnly;
                OpenLibrary.Image = Icon.IconImage.Library;
            }

            if (AddFolder != null)
            {
                AddFolder.ShowIcon = Button.ButtonStyle.IconOnly;
                AddFolder.Image = Icon.IconImage.Folder;
            }

            if (Delete != null)
            {
                Delete.ShowIcon = Button.ButtonStyle.IconOnly;
                Delete.Image = Icon.IconImage.Trash;
            }

            if (AddToLibrary != null)
            {
                AddToLibrary.ShowIcon = Toggle.ToggleStyle.IconOnly;
                AddToLibrary.Image = Icon.IconImage.LibraryAdd;
            }

            if (AddLayer != null)
            {
                AddLayer.ShowIcon = Toggle.ToggleStyle.IconOnly;
                AddLayer.Image = Icon.IconImage.Plus;
            }
        }
    }
}
