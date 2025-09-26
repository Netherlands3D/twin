using UnityEngine.UI;

namespace Netherlands3D.Twin.Layers.Properties
{
    //a customized button class to still use the unity visuals
    public class HiddenObjectsSelectableButton : Button
    {
        private bool forceSelected = false; 
        private bool pointerOver = false;

        public void ForceVisualSelection(bool selected)
        {
            forceSelected = selected;

            var selectable = this as Selectable;
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
                if (pointerOver)
                    base.DoStateTransition(SelectionState.Highlighted, instant);
                else
                    base.DoStateTransition(SelectionState.Pressed, instant);
            }
            else
            {
                if (pointerOver)
                    base.DoStateTransition(state, instant);
                else
                    base.DoStateTransition(SelectionState.Normal, instant);
            }
        }
       
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
    }
}
