using System;
using System.Collections.Generic;
using System.Linq;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine;
using UnityEngine.InputSystem;
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

            // RegisterCallback<PointerDownEvent>(OnClick, TrickleDown.TrickleDown);
            selectionChanged += OnSelectionChanged;
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
            if (selectionType != SelectionType.Multiple) return;

            // var el = activeElement as VisualElement;
            // //find upwards in the tree until unitylistview item is not found which means we will have the listview parent
            // while (el != null && !el.ClassListContains("unity-collection-view__item"))
            //     el = el.parent;
            // if (el == null) return;

            int targetIndex = indexDictionary[hoveredElement];
            if (!Keyboard.current.shiftKey.isPressed)
            {
                //update the selection start reference
                firstSelectedIndex = targetIndex;
                if(!lastSelectedIndices.Contains(targetIndex))
                    lastSelectedIndices.Add(targetIndex);
                return;
            }

            int firstIndex = firstSelectedIndex;
            //new selection indices from the listview selection
            var newSelection = selectedIndices.ToList();

            //did the previous selection have the new clicked element? if so then deselect elements until the start reference
            if (lastSelectedIndices.Contains(targetIndex))
            {
                //we need to know the last selected direction in case the clicked position is equal to the start reference
                //is the start reference index lower than the clicked index OR was the previous direction ascending and clicked at same position as the start
                if (firstIndex < targetIndex || (firstIndex == targetIndex && lastDirection > 0))
                {
                    for (int i = firstIndex + 1; i < itemsSource.Count; i++)
                        if (lastSelectedIndices.Contains(i))
                            lastSelectedIndices.Remove(i);
                        else
                            break;
                }

                //is the start reference index higher than the clicked index OR was the previous direction descending and clicked at same position as the start
                if (firstIndex > targetIndex || (firstIndex == targetIndex && lastDirection < 0))
                {
                    for (int i = firstIndex - 1; i >= 0; i--)
                        if (lastSelectedIndices.Contains(i))
                            lastSelectedIndices.Remove(i);
                        else
                            break;
                }
            }

            //the listview selected indices dont match the current selection (it wants to select everything no matter what we do, so we need to bring back the current selection)
            int min = Mathf.Min(firstIndex, targetIndex);
            int max = Mathf.Max(firstIndex, targetIndex);
            for (int i = 0; i < itemsSource.Count; i++)
            {
                if (i < min || i > max)
                {
                    if (!lastSelectedIndices.Contains(i))
                        newSelection.Remove(i);
                }
            }

            //cache the lastdirection in case the next selection is clicked at the same position as the starting reference
            //update the start reference within the bounds of the newest selection group, so we dont select the whole selection and keep the gaps
            if (firstIndex < targetIndex)
            {
                lastDirection = 1;
                for (int i = targetIndex - 1; i >= 0; i--)
                    if (!newSelection.Contains(i))
                    {
                        firstSelectedIndex = (i + 1);
                        break;
                    }
            }
            else if (firstIndex > targetIndex)
            {
                lastDirection = -1;
                for (int i = targetIndex + 1; i < itemsSource.Count; i++)

                    if (!newSelection.Contains(i))
                    {
                        firstSelectedIndex = (i - 1);
                        break;
                    }
            }

            SetSelection(newSelection);
            lastSelectedIndices.Clear();
            lastSelectedIndices.AddRange(newSelection);
        }
    }
}