using Netherlands3D.Events;
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
        [SerializeField] private BoolEvent toggleImportPanel;

        private void Awake()
        {
            button = GetComponent<Button>();
        }
        
        public void OpenAddLayerPanel()
        {
            if (!layersTool.Open)
            {
                layersButton.Toggle(); //open the panel if it's not open yet
            } 
            toggleImportPanel.InvokeStarted(layersTool.Open);
        }
    }
}
