using System;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class InspectorPanel : VisualElement, IContainer
    {
        private Label Header => this.Q<Label>(className: "inspector-header-title");

        /// <summary>
        /// Header text pass-through so it can be set from UXML/Inspector.
        /// </summary>
        [UxmlAttribute("header-text")]
        public string HeaderText
        {
            get => Header?.text;
            set { if (Header != null) Header.text = value; }
        }

        private ToolbarInspector toolbar;
        public ToolbarInspector Toolbar => toolbar ??= this.Q<ToolbarInspector>();
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

        public VisualElement Content => this.Q("Content");

        public InspectorPanel()
        {
            this.CloneComponentTree("Panels");
            this.AddComponentStylesheet("Panels");

            RegisterCallback<AttachToPanelEvent>(_ =>
            {
                Toolbar.Style = _toolbarStyleCache;
            });
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
