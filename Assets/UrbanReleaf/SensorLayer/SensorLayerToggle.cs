using Netherlands3D.Twin.UI.LayerInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Netherlands3D.Twin
{
    public class SensorLayerToggle : StandardLayerToggle
    {
        protected override void OnEnable()
        {
            layerUIManager = FindObjectOfType<LayerUIManager>();
            base.OnEnable();
        }
    }
}
