using System;
using System.Collections.Generic;
using Netherlands3D.Twin.Layers;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class TreeView : UnityEngine.UIElements.TreeView
    {
        private Action<VisualElement, int> _userBind;

        private int firstSelectedIndex = -1;
        private int lastDirection = 0;
        private VisualElement hoveredElement;
        private List<int> lastSelectedIndices = new();
        private readonly Dictionary<VisualElement, int> indexDictionary = new Dictionary<VisualElement, int>();

        /// <summary>
        /// Intercept bindItem so we can apply inline fixes after user binding.
        /// </summary>
        public new Action<VisualElement, int> bindItem
        {
            get => _userBind;
            set
            {
                _userBind = value;
                base.bindItem = OnBindItem;
            }
        }

        private void OnBindItem(VisualElement ve, int id)
        {
            indexDictionary[ve] = id;
            ve.RegisterCallback<PointerEnterEvent>(SetActiveElement);

            _userBind?.Invoke(ve, id);
        }

        private void SetActiveElement(PointerEnterEvent evt)
        {
            hoveredElement = evt.target as VisualElement;
        }

        public TreeView()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");

            selectionChanged += OnSelectionChanged;
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }
        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            LayerTreeViewUtility.ReleaseListsToPool(this);
        }
        
        private void OnSelectionChanged(IEnumerable<object> obj)
        {
            this.OnSelectionChanged(
                hoveredElement,
                indexDictionary,
                lastSelectedIndices,
                ref firstSelectedIndex,
                ref lastDirection);
        }
    }
}