using Netherlands3D.Twin.Layers.UI.AddLayer;
using Netherlands3D.Twin.Tools.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D.Twin.Tools
{
    public class OpenAddLayerPanelButton : MonoBehaviour
    {
        [SerializeField] private Tool layersTool;
        [SerializeField] private ToolButton layersButton;
        private Button button;

        private void Awake()
        {
            button = GetComponent<Button>();
        }
        
        public void OpenAddLayerPanel()
        {
            if(!layersTool.Open)
                layersButton.Toggle(); //open the panel if it's not open yet
            
            FindAnyObjectByType<AddLayerPanel>().TogglePanel(true); //ugly, but easy way to open the add layer panel. 
        }
    }
}
