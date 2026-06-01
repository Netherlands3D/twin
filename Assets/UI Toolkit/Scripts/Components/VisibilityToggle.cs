using System;
using Netherlands3D.UI_Toolkit;
using Netherlands3D.UI_Toolkit.Scripts;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    public enum VisibilityState
    {
        Visible = IconImage.Visibility, 
        Invisible = IconImage.Invisible, 
        PartiallyVisible = IconImage.VisibilityMixed,
        VisibleInInvisible = IconImage.VisibleInInvisible
    }
    
    [UxmlElement]
    public partial class VisibilityToggle : UnityEngine.UIElements.Toggle
    {
        // Query and cache icon component
        private Icon icon;
        private Icon Icon => icon ??= this.Q<Icon>("Icon");
        
        [UxmlAttribute("eye-state")]
        public VisibilityState Image
        {
            get => (VisibilityState)Icon.Image;
            set => Icon.Image = (IconImage)value;
        }

        public VisibilityToggle()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");
            
            this.RegisterValueChangedCallback(OnValueChanged);
        }

        private void OnValueChanged(ChangeEvent<bool> evt)
        {
            SetStateFromLayerState(evt.newValue, true,  true); //we cannot calculate the true state from the toggle without the rest of the hierarchy, this should be done from the tree if needed
        }

        public void Show(bool show)
        {
            EnableInClassList(UtilityClassConstants.HIDDEN, !show);
        }

        public void SetState(VisibilityState state)
        {
            Image = state;
        }
        
        public void SetStateFromLayerState(bool activeSelf, bool activeInHierarchy, bool allChildrenActive)
        {
            if (!activeSelf)
            {
                SetState(VisibilityState.Invisible);
            }
            else if (activeSelf && !activeInHierarchy)
            {
                SetState(VisibilityState.VisibleInInvisible);
            }
            else if (allChildrenActive)
            {
                SetState(VisibilityState.Visible);
            }
            else
            {
                SetState(VisibilityState.PartiallyVisible);
            }
        }
    }
}