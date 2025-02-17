using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Netherlands3D.Twin.Layers.Properties
{
    public class LayerWithUrl : MonoBehaviour, ILayerWithPropertyData
    {
        public LayerURLPropertyData UrlPropertyData = new LayerURLPropertyData() { Data = new Uri("https://netherlands3d.eu/") };
        LayerPropertyData ILayerWithPropertyData.PropertyData => UrlPropertyData;

        public void LoadProperties(List<LayerPropertyData> properties)
        {
            var urlProperty = (LayerURLPropertyData)properties.FirstOrDefault(p => p.GetType() == typeof(LayerURLPropertyData)); //use explicit check to avoid confusion with LayerImageURLPropertyData 
            if (urlProperty != null)
            {
                UrlPropertyData = urlProperty; //take existing property to overwrite the unlinked one of this class
            }
        }
    }
}