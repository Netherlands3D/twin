using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class InspectorPanel : VisualElement
    {
        private Label Header => this.Q<Label>(className: "inspector-header");

        /// <summary>
        /// Header text pass-through so it can be set from UXML/Inspector.
        /// </summary>
        [UxmlAttribute("header-text")]
        public string HeaderText
        {
            get => Header?.text;
            set { if (Header != null) Header.text = value; }
        }

        private ToolbarInspector Toolbar => this.Q<ToolbarInspector>();
        private ToolbarInspector.ToolbarStyle _toolbarStyleCache = ToolbarInspector.ToolbarStyle.Normal;

        /// <summary>
        /// Forwards the toolbar style to the child ToolbarInspector component.
        /// </summary>
        [UxmlAttribute("toolbar-style")]
        public ToolbarInspector.ToolbarStyle ToolbarStyle
        {
            get => Toolbar != null ? Toolbar.Style : _toolbarStyleCache;
            set
            {
                _toolbarStyleCache = value;
                if (Toolbar != null) Toolbar.Style = value;
            }
        }

        /// <summary>
        /// Optional convenience accessor for the content placeholder.
        /// </summary>
        public VisualElement Content => this.Q("Content");

        public InspectorPanel()
        {
            // Find and load UXML template for this component
            var asset = Resources.Load<VisualTreeAsset>("UI/Panels/" + nameof(InspectorPanel));
            asset.CloneTree(this);

            // Find and load USS stylesheet specific for this component (using -style)
            var styleSheet = Resources.Load<StyleSheet>("UI/Panels/" + nameof(InspectorPanel) + "-style");
            styleSheets.Add(styleSheet);

            RegisterCallback<AttachToPanelEvent>(_ =>
            {
                // Apply cached toolbar style when child is available
                if (Toolbar != null) Toolbar.Style = _toolbarStyleCache;
            });
        }
    }
}
