using System;
using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Twin.Layers.Properties;
using UnityEngine;

namespace Netherlands3D.Twin
{
    public class LayerWithImage : MonoBehaviour, ILayerWithPropertyData
    {
        public LayerImageURLPropertyData UrlPropertyData = new LayerImageURLPropertyData() { Data = new Uri("https://netherlands3d.eu/docs/handleiding/imgs/lagen.main.bottom.full.png") };
        LayerPropertyData ILayerWithPropertyData.PropertyData => UrlPropertyData;

        public void LoadProperties(List<LayerPropertyData> properties)
        {
            var urlProperty = (LayerImageURLPropertyData)properties.FirstOrDefault(p => p.GetType() == typeof(LayerImageURLPropertyData));
            if (urlProperty != null)
            {
                UrlPropertyData = urlProperty; //take existing property to overwrite the unlinked one of this class
            }
        }
    }
}