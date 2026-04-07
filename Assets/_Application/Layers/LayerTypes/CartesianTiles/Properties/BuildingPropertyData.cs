using System.Collections.Generic;
using System.Runtime.Serialization;
using Netherlands3D.SubObjects;
using Netherlands3D.Twin.Layers.Properties;
using Newtonsoft.Json;

namespace Netherlands3D.Twin.Layers.LayerTypes.CartesianTiles.Properties
{
    [DataContract(Namespace = "https://netherlands3d.eu/schemas/projects/layers/properties", Name = "Building")]
    public class BuildingPropertyData : LayerPropertyData
    {
        [JsonIgnore] public readonly List<string> buildingIds = new();
    }
}