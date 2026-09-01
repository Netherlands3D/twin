using Netherlands3D.UI_Toolkit.Scripts;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class CloseButton : UnityEngine.UIElements.Button
    {
        private Icon Icon => this.Q<Icon>("Icon");

        [UxmlAttribute("icon")]
        public string Image
        {
            get => Icon.Image;
            set => Icon.Image = value;
        }
        
        public CloseButton()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");
        }
    }
}