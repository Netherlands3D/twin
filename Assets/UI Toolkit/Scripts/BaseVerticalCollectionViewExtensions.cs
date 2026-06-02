using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    public static class BaseVerticalCollectionViewExtensions
    {
        public static void OnSelectionChanged(
            this BaseVerticalCollectionView view,
            VisualElement hoveredElement,
            Dictionary<VisualElement, int> indexDictionary,
            List<int> lastSelectedIndices,
            ref int firstSelectedIndex,
            ref int lastDirection)
        {
            if (view.selectionType != SelectionType.Multiple) return;

            int targetIndex = indexDictionary[hoveredElement];

            if (!Keyboard.current.shiftKey.isPressed)
            {
                firstSelectedIndex = targetIndex;
                if (!lastSelectedIndices.Contains(targetIndex))
                    lastSelectedIndices.Add(targetIndex);
                else
                    lastSelectedIndices.Remove(targetIndex);

                firstSelectedIndex = view.selectedIndices
                    .OrderBy(i => Math.Abs(i - targetIndex))
                    .FirstOrDefault();
                return;
            }

            int firstIndex = firstSelectedIndex;
            var newSelection = view.selectedIndices.ToList();

            if (lastSelectedIndices.Contains(targetIndex))
            {
                if (firstIndex < targetIndex || (firstIndex == targetIndex && lastDirection > 0))
                {
                    for (int i = firstIndex + 1; i < view.itemsSource.Count; i++)
                        if (lastSelectedIndices.Contains(i)) lastSelectedIndices.Remove(i);
                        else break;
                }

                if (firstIndex > targetIndex || (firstIndex == targetIndex && lastDirection < 0))
                {
                    for (int i = firstIndex - 1; i >= 0; i--)
                        if (lastSelectedIndices.Contains(i)) lastSelectedIndices.Remove(i);
                        else break;
                }
            }

            int min = Mathf.Min(firstIndex, targetIndex);
            int max = Mathf.Max(firstIndex, targetIndex);
            for (int i = 0; i < view.itemsSource.Count; i++)
                if ((i < min || i > max) && !lastSelectedIndices.Contains(i))
                    newSelection.Remove(i);

            if (firstIndex < targetIndex)
            {
                lastDirection = 1;
                for (int i = targetIndex - 1; i >= 0; i--)
                    if (!newSelection.Contains(i)) { firstSelectedIndex = i + 1; break; }
                    else if (i == 0) firstSelectedIndex = 0;
            }
            else if (firstIndex > targetIndex)
            {
                lastDirection = -1;
                for (int i = targetIndex + 1; i < view.itemsSource.Count; i++)
                    if (!newSelection.Contains(i)) { firstSelectedIndex = i - 1; break; }
                    else if (i == view.itemsSource.Count - 1) firstSelectedIndex = view.itemsSource.Count - 1;
            }

            view.SetSelection(newSelection);
            lastSelectedIndices.Clear();
            lastSelectedIndices.AddRange(newSelection);
        }
    }
}