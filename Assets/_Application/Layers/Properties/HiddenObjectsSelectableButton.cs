using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D.Twin.Layers.Properties
{
    public class HiddenObjectsSelectableButton : Button
    {
        public void ForceVisualSelection(bool selected)
        {
            DoStateTransition(
                selected ? SelectionState.Selected : SelectionState.Normal,
                false
            );
        }
    }
}
