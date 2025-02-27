using Netherlands3D.E2ETesting.PageObjectModel;
using UnityEngine.UI;

namespace Netherlands3D.E2ETesting.UI
{
    public class ButtonElement : Element<Button>
    {
        public ButtonElement(Button value) : base(value)
        {
        }
        
        public void Click()
        {
            Value.onClick.Invoke();
        }
    }
}