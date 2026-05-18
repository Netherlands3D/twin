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
        private int _firstSelectedIndex = -1;
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
            _userBind?.Invoke(ve, id);
            indexDictionary[ve] = id;
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
            var clickedIndex = viewController.GetIndexForId(index);
            
            if (!evt.shiftKey)
            {
                _firstSelectedIndex = clickedIndex;
                return;
            }
            
            ProcessSelectionWithShift(evt, clickedIndex);
        }
        
        private void ProcessSelectionWithShift(ClickEvent evt, int clickedIndex)
        {
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
            Debug.Log();
            int lastSelectedIndex = selectedIndices.Max();

            bool addSelection = !selectedIndices.Contains(targetIndex);

            var newSelection = selectedIndices.ToList();

            if (!addSelection)
            {
                //Items need to be sequentially removed until the cursor 
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