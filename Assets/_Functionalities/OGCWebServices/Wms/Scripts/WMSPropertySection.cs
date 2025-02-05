using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D.Functionalities.Wms
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
                    legendToggle.onValueChanged.AddListener(controller.SetLegendActive);
                    controller.SetLegendActive(legendToggle.isOn);
                }
            }
        }
       

        private void OnDestroy()
        {
            controller.SetLegendActive(false);
            if (controller != null)
                legendToggle.onValueChanged.RemoveListener(controller.SetLegendActive);
        }
    }
}
