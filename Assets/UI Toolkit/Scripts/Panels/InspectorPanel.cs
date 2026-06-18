using System;
using Netherlands3D.UI_Toolkit;
using Netherlands3D.UI_Toolkit.Scripts.Panels;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class InspectorPanel : VisualElement
    {
        public Action OnShow;
        public Action OnHide;
        
        private Label header;
        private Label Header => header ??= this.Q<Label>(className: "inspector-header-title");
        private Button inspectorHeaderCloseButton;
        public Button InspectorHeaderCloseButton => inspectorHeaderCloseButton ??= this.Q<Button>("InspectorHeaderCloseButton");

        /// <summary>
        /// Header text pass-through so it can be set from UXML/Inspector.
        /// </summary>
        [UxmlAttribute("header-text")]
        public string HeaderText
        {
            get => Header?.text;
            set { if (Header != null) Header.text = value; }
        }               

        public VisualElement Content => this.Q("Content");

        public InspectorPanel()
        {
            this.CloneComponentTree("Panels");
            this.AddComponentStylesheet("Panels");           
        }

        public void Open()
        {
            EnableInClassList(UtilityClassConstants.HIDDEN, false);
            OnShow?.Invoke();
        }

        public void Close()
        {
            EnableInClassList(UtilityClassConstants.HIDDEN, true);
            OnHide?.Invoke();
        }

        public bool IsOpen() => !ClassListContains(UtilityClassConstants.HIDDEN);

        public void AddContent(BaseInspectorContentPanel content)
        {
            Content.Add(content);
        }
        
        public void ClearContent()
        {
            Content.Clear();
        }
    }
}
