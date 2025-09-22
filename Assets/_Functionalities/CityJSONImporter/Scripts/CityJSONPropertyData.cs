using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Netherlands3D.Twin.Layers.Properties;
using Newtonsoft.Json;
using UnityEngine.Events;

namespace Netherlands3D.Functionalities.CityJSON
{
    [DataContract(Namespace = "https://netherlands3d.eu/schemas/projects/layers/properties", Name = "CityJson")]
    public class CityJSONPropertyData : LayerPropertyData, ILayerPropertyDataWithAssets
    {
        [DataMember] private Uri cityJsonFile;
        [JsonIgnore] public readonly UnityEvent<Uri> CityJsonUriChanged = new();

        [JsonIgnore]
        public Uri CityJsonFile
        {
            get => cityJsonFile;
            set
            {
                cityJsonFile = value;
                CityJsonUriChanged.Invoke(value);
            }
        }

        public IEnumerable<LayerAsset> GetAssets()
        {
            var existingAssets = new List<LayerAsset>();

            if (cityJsonFile != null)
            {
                existingAssets.Add(new(this, cityJsonFile));
            }

            return existingAssets;
        }
    }
}