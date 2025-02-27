using Netherlands3D.E2ETesting.PageObjectModel;
using UnityEngine.UI;

namespace Netherlands3D.E2ETesting.UI
{
    public class ToggleElement : Element<Toggle, ToggleElement>
    {
        public bool IsOn => Value.isOn;

        public void Toggle()
        {
            Value.isOn = !Value.isOn;
        }
    }
}