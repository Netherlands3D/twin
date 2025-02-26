using UnityEngine.UI;

namespace Netherlands3D
{
    public static partial class E2E
    {
        public static partial class Given
        {
            public static void LayerPanelIsOpen()
            {
                FindComponentOnGameObject<Button>("ToolbarButton_Layers").Click();
            }
        }
    }
}