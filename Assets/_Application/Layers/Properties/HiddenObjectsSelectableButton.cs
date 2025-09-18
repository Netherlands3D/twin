using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D.Twin.Layers.Properties
{
    public class HiddenObjectsSelectableButton : Button
    {
        public void ForceVisualSelection(bool selected)
        {
            forceSelected = selected;
            DoStateTransition(selected ? SelectionState.Selected : SelectionState.Normal, false);
        }

        private bool forceSelected = false;

        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            if (forceSelected)
            {
                // Always stay in Selected
                base.DoStateTransition(SelectionState.Selected, instant);
            }
            else
            {
                base.DoStateTransition(state, instant);
            }
        }
    }
}
