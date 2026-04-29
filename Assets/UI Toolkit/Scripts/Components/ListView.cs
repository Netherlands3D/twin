using System;
using System.Linq;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class ListView : UnityEngine.UIElements.ListView
    {
        // Keep user bind so we can call it first.
        private Action<VisualElement, int> _userBind;
        private int _firstSelectedIndex = -1;

        /// <summary>
        /// Intercept bindItem so we can apply inline fixes after user binding.
        /// </summary>
        public new Action<VisualElement, int> bindItem
        {
            get => _userBind;
            set
            {
                _userBind = value;
                base.bindItem = (ve, i) =>
                {
                    _userBind?.Invoke(ve, i);
                    ve.userData = i;
                };
            }
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
            
            RegisterCallback<ClickEvent>(OnPointerDown, TrickleDown.TrickleDown);
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
        
        private void OnPointerDown(ClickEvent evt)
        {
            if (selectionType != SelectionType.Multiple) return;

            var el = evt.target as VisualElement;
            //find upwards in the tree until unitylistview item is not found which means we will have the listview parent
            while (el != null && !el.ClassListContains("unity-list-view__item"))
                el = el.parent;
            if (el == null) return;

            var clickedIndex = (int)el.userData;

            if (!evt.shiftKey)
            {
                _firstSelectedIndex = clickedIndex;
                return;
            }

            var selectedIndices = this.selectedIndices.ToList();
            if (selectedIndices.Count == 0)
            {
                _firstSelectedIndex = clickedIndex;
                this.SetSelectionWithoutNotify(new[] { clickedIndex });
                evt.StopPropagation();
                return;
            }

            int firstIndex = _firstSelectedIndex;
            int targetIndex = clickedIndex;
            int lastSelectedIndex = selectedIndices.Max();

            bool addSelection = !selectedIndices.Contains(targetIndex);

            var newSelection = selectedIndices.ToList();

            if (!addSelection)
            {
                if (firstIndex < targetIndex)
                    for (int i = targetIndex + 1; i <= lastSelectedIndex; i++)
                        newSelection.Remove(i);
                else if (firstIndex > targetIndex)
                    for (int i = selectedIndices.Min(); i < targetIndex; i++)
                        newSelection.Remove(i);
                else if (firstIndex == targetIndex)
                    newSelection.RemoveAll(i => i != targetIndex);
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

            SetSelection(newSelection);
            evt.StopPropagation();
        }
    }
}