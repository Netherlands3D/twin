using System;
using System.Collections.Generic;
using System.Linq;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class TreeView : UnityEngine.UIElements.TreeView
    {
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
            var collectionViewItem = ve;
            while (collectionViewItem != null && !collectionViewItem.ClassListContains("unity-collection-view__item"))
                collectionViewItem = collectionViewItem.parent;
            if (collectionViewItem != null) 
                indexDictionary[collectionViewItem] = id;
    
            _userBind?.Invoke(ve, id);
        }

        
        public TreeView()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");
            
            RegisterCallback<ClickEvent>(OnClick, TrickleDown.TrickleDown);
        }
        
        private void OnClick(ClickEvent evt)
        {
            if (selectionType != SelectionType.Multiple) return;

            var el = evt.target as VisualElement;
            //find upwards in the tree until unitylistview item is not found which means we will have the listview parent
            while (el != null && !el.ClassListContains("unity-collection-view__item"))
                el = el.parent;
            if (el == null) return;

            var index = indexDictionary[el];
            Debug.Log("index for element:" + index);
            var clickedIndex = viewController.GetIndexForId(index);
            
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