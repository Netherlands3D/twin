using System;
using System.Collections.Generic;
using System.Linq;
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
            _userBind?.Invoke(ve, id);
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
            
            RegisterCallback<ClickEvent>(OnClick, TrickleDown.TrickleDown);
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
        
        private void OnClick(ClickEvent evt)
        {
            if (selectionType != SelectionType.Multiple) return;

            var el = evt.target as VisualElement;
            //find upwards in the tree until unitylistview item is not found which means we will have the listview parent
            while (el != null && !el.ClassListContains("unity-collection-view__item"))
                el = el.parent;
            if (el == null) return;

            var clickedIndex = indexDictionary[el];
            if (!evt.shiftKey)
            {
                firstSelectedIndex = clickedIndex;
                return;
            }

            int firstIndex = firstSelectedIndex;
            int targetIndex = clickedIndex;
            var newSelection = selectedIndices.ToList();

            if (selectedIndices.Contains(targetIndex))
            {
                if (firstIndex <= targetIndex)
                    for (int i = targetIndex + 1; i <= selectedIndices.Max(); i++)
                        newSelection.Remove(i);
                if (firstIndex >= targetIndex)
                    for (int i = selectedIndices.Min(); i < targetIndex; i++)
                        newSelection.Remove(i);
            }
            else
            {
                if (firstIndex < targetIndex)
                {
                    for (int i = firstIndex; i <= targetIndex; i++)
                        if (!newSelection.Contains(i))
                            newSelection.Add(i);
                }
                else if (firstIndex > targetIndex)
                {
                    for (int i = targetIndex; i <= firstIndex; i++)
                        if (!newSelection.Contains(i))
                            newSelection.Add(i);
                }
            }
            
            if(lastSelectedIndices.Count > 0 && !lastSelectedIndices.Contains(targetIndex))
            {
                if (firstIndex < targetIndex)
                    firstSelectedIndex = newSelection.Min();
                else if (firstIndex > targetIndex)
                    firstSelectedIndex = newSelection.Max();
            }
        
            SetSelection(newSelection);
            lastSelectedIndices.Clear();
            lastSelectedIndices.AddRange(newSelection);
            evt.StopPropagation();
        }
    }
}