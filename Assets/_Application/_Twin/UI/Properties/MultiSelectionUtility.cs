using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Netherlands3D.Twin.UI
{
    public static class MultiSelectionUtility 
    {
        public static bool AddToSelectionModifierKeyIsPressed()
        {
            if (SystemInfo.operatingSystemFamily == OperatingSystemFamily.MacOSX)
            {
                return Keyboard.current.leftCommandKey.isPressed || Keyboard.current.rightCommandKey.isPressed;
            }

            return Keyboard.current.ctrlKey.isPressed;
        }

        public static bool SequentialSelectionModifierKeyIsPressed()
        {
            return Keyboard.current.shiftKey.isPressed;
        }

        public static bool NoModifierKeyPressed()
        {
            return !AddToSelectionModifierKeyIsPressed() && !SequentialSelectionModifierKeyIsPressed();
        }

        public static void ProcessLayerSelection(IMultiSelectable container, Action<bool> anythingSelected)
        {
            bool anySelected = false;
            if (SequentialSelectionModifierKeyIsPressed())
            {
                if (container.SelectedItems.Count > 0)
                {
                    int firstSelectedIndex = container.Items.IndexOf(container.SelectedItems[0]);
                    int lastSelectedIndex = container.Items.IndexOf(container.SelectedItems[container.SelectedItems.Count - 1]);
                    int targetIndex = container.SelectedButtonIndex;
                    int firstIndex = container.Items.IndexOf(container.FirstSelectedItem);

                    bool addSelection = !container.Items[container.SelectedButtonIndex].IsSelected;
                    if (!addSelection)
                    {
                        if (firstIndex < targetIndex)
                            for (int i = targetIndex + 1; i <= lastSelectedIndex; i++)
                                container.Items[i].SetSelected(addSelection);
                        else if (firstIndex > targetIndex)
                            for (int i = 0; i < targetIndex; i++)
                                container.Items[i].SetSelected(addSelection);
                        else if (firstIndex == targetIndex)
                            for (int i = 0; i <= lastSelectedIndex; i++)
                                if (i != container.SelectedButtonIndex)
                                    container.Items[i].SetSelected(addSelection);
                    }
                    else
                    {
                        //we use the first selected item to only select the range for mutli select and not the last selected item when some are not selected in between
                        if (firstIndex < targetIndex)
                            for (int i = firstIndex; i <= targetIndex; i++)
                                container.Items[i].SetSelected(addSelection);
                        else if (firstIndex > targetIndex)
                            for (int i = targetIndex; i <= firstIndex; i++)
                                container.Items[i].SetSelected(addSelection);
                    }
                }
                else
                {
                    anySelected = true;
                    container.Items[container.SelectedButtonIndex].SetSelected(true);
                }
            }
            else if (AddToSelectionModifierKeyIsPressed())
            {
                container.Items[container.SelectedButtonIndex].SetSelected(!container.Items[container.SelectedButtonIndex].IsSelected);
                if (container.Items[container.SelectedButtonIndex].IsSelected)
                {
                    anySelected = true;
                    container.FirstSelectedItem = container.Items[container.SelectedButtonIndex];
                }
            }
            if (NoModifierKeyPressed())
            {
                foreach (var item in container.Items)
                    item.SetSelected(false);

                //are we toggling the previous selected only item?
                if (container.SelectedItems.Count != 1 || container.SelectedItems[0] != container.Items[container.SelectedButtonIndex])
                {
                    anySelected = true;
                    container.Items[container.SelectedButtonIndex].SetSelected(true);
                }
            }

            //refresh selected items
            container.SelectedItems.Clear();
            foreach (ISelectable item in container.Items)
                if (item.IsSelected)
                    container.SelectedItems.Add(item);
            if (container.SelectedItems.Count == 0)
                container.FirstSelectedItem = null;

            if (anySelected)
            {
                //cache the first selected item for sequential selection to always know where to start
                if (container.SelectedItems.Count == 0 || (container.SelectedItems.Count == 1 && container.FirstSelectedItem != container.Items[container.SelectedButtonIndex]))
                    container.FirstSelectedItem = container.Items[container.SelectedButtonIndex];

                anythingSelected?.Invoke(true);                
            }
            else if (container.SelectedItems.Count == 0)
            {               
                anythingSelected?.Invoke(false);
            }
        }
    }
}
