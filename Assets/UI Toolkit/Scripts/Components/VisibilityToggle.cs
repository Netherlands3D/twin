using System;
using System.Collections.Generic;
using System.Linq;
using Netherlands3D.UI_Toolkit;
using Netherlands3D.UI_Toolkit.Scripts;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    public enum VisibilityState
    {
        Visible,
        Invisible,
        PartiallyVisible,
        VisibleInInvisible
        
        // Visible = IconImage.Visibility, 
        // Invisible = IconImage.Invisible, 
        // PartiallyVisible = IconImage.VisibilityMixed,
        // VisibleInInvisible = IconImage.VisibleInInvisible
    }
    
    [UxmlElement]
    public partial class VisibilityToggle : UnityEngine.UIElements.Toggle
    {
        private static readonly Dictionary<VisibilityState, string> visibilityStateMap = new()
        {
            { VisibilityState.Visible, IconImage.VISIBILITY},
            { VisibilityState.Invisible, IconImage.INVISIBLE },
            { VisibilityState.PartiallyVisible, IconImage.VISIBILITY_MIXED },
            { VisibilityState.VisibleInInvisible, IconImage.VISIBLE_IN_INVISIBLE }
        };

        public static string GetIconImage(VisibilityState state)
        {
            return visibilityStateMap[state];
        }
        
        // Query and cache icon component
        private Icon icon;
        private Icon Icon => icon ??= this.Q<Icon>("Icon");
        
        [UxmlAttribute("eye-state")]
        public VisibilityState Image
        {
            get => visibilityStateMap.Keys.FirstOrDefault(k => visibilityStateMap[k] == Icon.Image); //todo: make this more reliable
            set => Icon.Image = GetIconImage(value);
        }

        public VisibilityToggle()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");
            SetStateFromLayerState(value, true,  true); //we cannot calculate the true state from the toggle without the rest of the hierarchy, this should be done from the tree if needed
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