using Newtonsoft.Json;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Netherlands3D.Twin.Layers.Properties
{
    [DataContract(Namespace = "https://netherlands3d.eu/schemas/projects/layers/properties", Name = "CartesianTileLayerFeatureColor")]
    public class CartesianTileLayerFeatureColorPropertyData : LayerPropertyData
    {       
        [JsonIgnore] public Dictionary<object, LayerFeature> LayerFeatures { get; private set; } = new();


        [JsonConstructor]
        public CartesianTileLayerFeatureColorPropertyData()
        {
           
        }
    }
}