using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D.Functionalities.Wms.UI
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
                    legendToggle.isOn = controller.ShowLegendOnSelect;
                    legendToggle.onValueChanged.AddListener(SetLegendActive);
                    SetLegendActive(legendToggle.isOn);
                }
            }
        }

        private void SetLegendActive(bool active)
        {
            controller.ShowLegendOnSelect = active;
            controller.SetLegendActive(active);
        }

        private void OnDestroy()
        {
            if (controller != null)
                legendToggle.onValueChanged.RemoveListener(controller.SetLegendActive);
        }
    }
}
