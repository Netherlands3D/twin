using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]

    public partial class ToggleButtonGroupPassthrough : ToggleButtonGroup
    {
        [UxmlAttribute("picking-mode")]
        public PickingMode PickingMode
        {
            get
            {
                return pickingMode;
            }
            set
            {
                pickingMode = value;
                this.Q(containerUssClassName).pickingMode = value; //containerUssClassName is a weird variable name, but it is being set as the container name in the base constructor so we will use it here
            }
        }
    }
}
