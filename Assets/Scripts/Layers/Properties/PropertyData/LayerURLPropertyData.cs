using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Netherlands3D.Twin.Layers.Properties
{
    [DataContract(Namespace = "https://netherlands3d.eu/schemas/projects/layers/properties", Name = "Url")]
    public class LayerURLPropertyData : LayerPropertyData, ILayerPropertyDataWithAssets
    {
        [DataMember] public string url = "";
        
        public IEnumerable<LayerAsset> GetAssets()
        {
            return new List<LayerAsset>()
            {
                new (this, !string.IsNullOrEmpty(url) ? new Uri(url) : null)
            };
        }
    }
}
