using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D.Functionalities.OgcWebServices.Wms.UI
{
    public class WMSPropertySection : MonoBehaviour
    {
        [SerializeField] private Toggle legendToggle; 
        private WMSLayerGameObject controller;
        public WMSLayerGameObject Controller
        {
            get
            {
                return controller;
            }
            set
            {
                controller = value;
                
                if (controller != null)
                {
                    legendToggle.isOn = controller.ShowLegend;
                    legendToggle.onValueChanged.AddListener(controller.SetLegendActive);
                    controller.SetLegendActive(legendToggle.isOn);
                }
            }
        }
       

        private void OnDestroy()
        {
            if (controller != null)
                legendToggle.onValueChanged.RemoveListener(controller.SetLegendActive);
        }
    }
}
