using System;
using System.Collections.Generic;
using System.Linq;
using Netherlands3D.UI.ExtensionMethods;
using UnityEditor.Graphs;
using UnityEngine;
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
        private List<int> lastSelectedIndices = new();

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
            selectedIndicesChanged += indices =>
            {
                List<int> newSelection = indices.ToList();
                // for (int i = 0; i < itemsSource.Count; i++)
                // {
                //     DebugIndex(i, false);
                // }
                //
                // foreach (int i in lastSelectedIndices)
                // {
                //     DebugIndex(i, true, Color.green);
                // }
                // foreach (int index in indices)
                // {
                //     if (!lastSelectedIndices.Contains(index))
                //         newSelection.Remove(index);
                // }
                //SetSelection(newSelection);
            };
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

        private void ClearDebug()
        {
            for (int i = 0; i < itemsSource.Count; i++)
            {
                GetRootElementForIndex(i).style.borderBottomColor = Color.clear;
                GetRootElementForIndex(i).style.borderBottomWidth = 0;
            }
        }
        
        private void ClearDebug2()
        {
            for (int i = 0; i < itemsSource.Count; i++)
            {
                GetRootElementForIndex(i).style.borderLeftColor = Color.clear;
                GetRootElementForIndex(i).style.borderLeftWidth = 0;
            }
        }
        
        private void DebugIndex(int index, Color debugColor = default)
        {
            GetRootElementForIndex(index).style.borderBottomColor = debugColor;
            GetRootElementForIndex(index).style.borderBottomWidth = 2;
        }
        
        private void DebugIndex2(int index, Color debugColor = default)
        {
            GetRootElementForIndex(index).style.borderLeftColor = debugColor;
            GetRootElementForIndex(index).style.borderLeftWidth = 2;
        }

        private void SetFirstSelectedIndex(int index)
        {
            ClearDebug();
            firstSelectedIndex = index;
            DebugIndex(index, Color.red);
        }
        
        private void OnPointerDown(ClickEvent evt)
        {
            if (selectionType != SelectionType.Multiple) return;

            var el = evt.target as VisualElement;
            //find upwards in the tree until unitylistview item is not found which means we will have the listview parent
            while (el != null && !el.ClassListContains("unity-list-view__item"))
                el = el.parent;
            if (el == null) return;

            int targetIndex = (int)el.userData;
            if (!evt.shiftKey)
            {
                SetFirstSelectedIndex(targetIndex);   
                return;
            }

            
            int firstIndex = firstSelectedIndex;
            var newSelection = selectedIndices.ToList();

            if (lastSelectedIndices.Contains(targetIndex))
            {
                if (firstIndex < targetIndex || (firstIndex == targetIndex && lastDirection > 0))
                {
                    for(int i = firstIndex + 1; i < itemsSource.Count; i++)
                        if (lastSelectedIndices.Contains(i))
                            lastSelectedIndices.Remove(i);
                        else
                            break;
                }
                if (firstIndex > targetIndex || (firstIndex == targetIndex && lastDirection < 0))
                {
                    for (int i = firstIndex - 1; i >= 0; i--)
                        if (lastSelectedIndices.Contains(i))
                            lastSelectedIndices.Remove(i);
                        else
                            break;
                }
            }
            
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
            
            if (firstIndex < targetIndex)
            {
                lastDirection = 1;
                for (int i = targetIndex - 1; i >= 0; i--)
                    if (!newSelection.Contains(i))
                    {
                        SetFirstSelectedIndex(i + 1);
                        break;
                    }
                
            }
            else if (firstIndex > targetIndex)
            {
                lastDirection = -1;
                for(int i = targetIndex + 1; i < itemsSource.Count; i++)
                
                    if (!newSelection.Contains(i))
                    {
                        SetFirstSelectedIndex(i - 1);
                        break;
                    }
            }
        
            SetSelection(newSelection);
            lastSelectedIndices.Clear();
            lastSelectedIndices.AddRange(newSelection);
            ClearDebug2();
            foreach (int i in lastSelectedIndices)
                DebugIndex2(i, Color.green);
            
            evt.StopPropagation();
        }
    }
}