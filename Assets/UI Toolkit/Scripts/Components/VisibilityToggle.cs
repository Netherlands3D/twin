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
        VisibleInInvisible //todo: not implemented yet
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
            SetImage(value);
        }

        private void OnValueChanged(ChangeEvent<bool> evt)
        {
            SetImage(evt.newValue);
        }

        private void SetImage(bool newValue)
        {
            Image = newValue ? VisibilityState.Visible : VisibilityState.Invisible;
            if (newValue)
                Icon.Color = ThemeColor.Blue900;
            else
                Icon.Color = ThemeColor.Blue200;
        }

        public void Show(bool show)
        {
            EnableInClassList(UtilityClassConstants.HIDDEN, !show);
        }

        public void SetState(VisibilityState state)
        {
            Image = state;
        }
    }
}