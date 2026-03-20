using System;
using Netherlands3D.UI_Toolkit.Scripts;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    public enum EyeState
    {
        Visible = IconImage.Visibility, 
        Invisible = IconImage.Invisible, 
        VisibleInInvisible, //todo: not implemented yet
        ContainsInvisible //todo: not implemented yet
    }
    
    [UxmlElement]
    public partial class EyeToggle : UnityEngine.UIElements.Toggle
    {
        // Query and cache icon component
        private Icon icon;
        private Icon Icon => icon ??= this.Q<Icon>("Icon");
        
        [UxmlAttribute("eye-state")]
        public EyeState Image
        {
            get => (EyeState)Icon.Image;
            set => Icon.Image = (IconImage)value;
        }

        public EyeToggle()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");
            
            SetImage(value);
            
            this.RegisterValueChangedCallback(OnValueChanged);
        }

        private void OnValueChanged(ChangeEvent<bool> evt)
        {
            SetImage(evt.newValue);
        }

        private void SetImage(bool newValue)
        {
            Image = newValue ? EyeState.Visible : EyeState.Invisible;
        }
    }
}