using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Twin.Layers.Properties;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace Netherlands3D.Twin
{
    public class LayerWithImage : MonoBehaviour, ILayerWithPropertyData
    {
        public LayerURLPropertyData UrlPropertyData = new LayerURLPropertyData() { Data = new Uri("https://netherlands3d.eu/docs/handleiding/imgs/lagen.main.bottom.full.png") };
        LayerPropertyData ILayerWithPropertyData.PropertyData => UrlPropertyData;

        public void LoadProperties(List<LayerPropertyData> properties)
        {
            var urlProperty = (LayerURLPropertyData)properties.FirstOrDefault(p => p is LayerURLPropertyData);
            if (urlProperty != null)
            {
                UrlPropertyData = urlProperty; //take existing property to overwrite the unlinked one of this class
            }
        }
    }
}