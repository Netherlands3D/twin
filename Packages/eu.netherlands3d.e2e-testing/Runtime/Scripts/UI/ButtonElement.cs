using Netherlands3D.E2ETesting.PageObjectModel;
using UnityEngine.UI;

namespace Netherlands3D.E2ETesting.UI
{
    public class ButtonElement : Element<Button, ButtonElement>
    {
        public void Click()
        {
            Value.onClick.Invoke();
        }
    }
}