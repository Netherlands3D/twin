using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D.Functionalities.Wms
{
    public class WMSPropertySection : MonoBehaviour
    {
        public WMSLayerGameObject Controller
        {
            get
            {
                return controller;
            }
            set
            {
                controller = value;
                
                legendToggle = GetComponentInChildren<Toggle>();
                if (legendToggle != null && controller != null)
                {
                    legendToggle.onValueChanged.AddListener(controller.SetLegendActive);
                    controller.SetLegendActive(legendToggle.isOn);
                }
            }
        }
       
        private WMSLayerGameObject controller;
        private Toggle legendToggle; 

        private void OnDestroy()
        {
            if (legendToggle != null && controller != null)
                legendToggle.onValueChanged.RemoveListener(controller.SetLegendActive);
        }
    }
}
