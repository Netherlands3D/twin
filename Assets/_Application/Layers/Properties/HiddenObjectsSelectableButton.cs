using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D.Twin.Layers.Properties
{
    public class HiddenObjectsSelectableButton : Button
    {
        private bool forceSelected = false;

        public void ForceVisualSelection(bool selected)
        {
            forceSelected = selected;

            var selectable = this as UnityEngine.UI.Selectable;
            if (selectable == null) return;

            if(forceSelected)
                selectable.Select();
            else
                selectable.OnDeselect(null);
        }

        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            if (forceSelected)
            {
                // Show Hover if pointer is over, else show Selected
                if (IsPointerOver())
                    base.DoStateTransition(SelectionState.Highlighted, instant);
                else
                    base.DoStateTransition(SelectionState.Pressed, instant);
            }
            else
            {
                if (IsPointerOver())
                    base.DoStateTransition(state, instant);
                else
                    base.DoStateTransition(SelectionState.Normal, instant);
            }
        }

        private bool pointerOver = false;

        public override void OnPointerEnter(UnityEngine.EventSystems.PointerEventData eventData)
        {
            base.OnPointerEnter(eventData);
            pointerOver = true;
            DoStateTransition(currentSelectionState, false);
        }

        public override void OnPointerExit(UnityEngine.EventSystems.PointerEventData eventData)
        {
            base.OnPointerExit(eventData);
            pointerOver = false;
            DoStateTransition(currentSelectionState, false);
        }

        private bool IsPointerOver() => pointerOver;
    }
}
