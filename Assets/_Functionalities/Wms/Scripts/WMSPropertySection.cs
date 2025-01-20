using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Projects;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D.Twin
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
