using System;
using System.Collections.Generic;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class ListView : UnityEngine.UIElements.ListView
    {
        // Keep user bind so we can call it first.
        private Action<VisualElement, int> _userBind;

        private int firstSelectedIndex = -1;
        private int lastDirection = 0;
        private VisualElement hoveredElement;
        private List<int> lastSelectedIndices = new();
        private readonly Dictionary<VisualElement, int> indexDictionary = new Dictionary<VisualElement, int>();

        [UxmlAttribute("empty-text")]
        public string EmptyText { get; set; } = "Deze lijst is leeg";

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
            var collectionViewItem = ve;
            while (collectionViewItem != null && !collectionViewItem.ClassListContains("unity-collection-view__item"))
                collectionViewItem = collectionViewItem.parent;
            if (collectionViewItem != null)
                indexDictionary[collectionViewItem] = id;

            ve.RegisterCallback<PointerEnterEvent>(SetActiveElement);

            _userBind?.Invoke(ve, id);
        }
        
        private void SetActiveElement(PointerEnterEvent evt)
        {
            hoveredElement = evt.target as VisualElement;
        }

        [UxmlAttribute("fixed-item-height")]
        public float FixedItemHeight
        {
            get => fixedItemHeight;
            set => fixedItemHeight = value;
        }

        [UxmlAttribute("selection-type")]
        public SelectionType SelectionMode
        {
            get => selectionType;
            set => selectionType = value;
        }

        [UxmlAttribute("virtualization-method")]
        public CollectionVirtualizationMethod VirtualizationMethod
        {
            get => virtualizationMethod;
            set => virtualizationMethod = value;
        }

        [UxmlAttribute("show-alternating-row-backgrounds")]
        public AlternatingRowBackground ShowAlternatingRowBackgrounds
        {
            get => showAlternatingRowBackgrounds;
            set => showAlternatingRowBackgrounds = value;
        }

        public ListView()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");

            // Defaults only if user code did not set factories
            if (makeItem == null) makeItem = CreateDefaultItem;
            if (base.bindItem == null) this.bindItem = DefaultBind;

            selectionChanged += OnSelectionChanged;
            
            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            var emptyLabel = this.Q<Label>(className: "unity-list-view__empty-label"); //the label only spawns after an empty list has been rendered
            if (emptyLabel != null)
                emptyLabel.text = EmptyText;
        }

        /// <summary>
        /// Default item: ListViewItem UXML with #Content and a Spacer (bottom).
        /// </summary>
        private VisualElement CreateDefaultItem()
        {
            return new ListViewItem();
        }

        /// <summary>
        /// Default bind does nothing; controllers populate #Content in their bindItem.
        /// </summary>
        private void DefaultBind(VisualElement item, int index)
        {
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