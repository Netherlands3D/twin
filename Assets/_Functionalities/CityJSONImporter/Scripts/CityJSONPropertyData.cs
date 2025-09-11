using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Netherlands3D.Coordinates;
using Netherlands3D.Twin.Layers.Properties;
using Newtonsoft.Json;
using UnityEngine.Events;

namespace Netherlands3D.Functionalities.CityJSON
{
    [DataContract(Namespace = "https://netherlands3d.eu/schemas/projects/layers/properties", Name = "CityJson")]
    public class CityJSONPropertyData : LayerPropertyData, ILayerPropertyDataWithAssets, ILayerPropertyDataWithCRS
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


        [DataMember] private int contentCRS = (int)CoordinateSystem.WGS84_ECEF;

        [JsonIgnore]
        public int ContentCRS
        {
            get => contentCRS;
            set
            {
                contentCRS = value;
                OnCRSChanged.Invoke(contentCRS);
            }
        }

        [JsonIgnore] public readonly UnityEvent<int> OnCRSChanged = new();
     
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