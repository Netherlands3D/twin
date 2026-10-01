using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Netherlands3D.Twin.Layers.Properties;
using Newtonsoft.Json;
using UnityEngine.Events;

namespace Netherlands3D.Functionalities.GeoTiff
{
    [DataContract(Namespace = "https://netherlands3d.eu/schemas/projects/layers/properties", Name = "GeoTiff")]
    public class GeoTiffPropertyData : LayerPropertyData, ILayerPropertyDataWithAssets
    {
        [DataMember] private Uri geoTiffFile;

        [JsonIgnore] public readonly UnityEvent<Uri> GeoTiffUriChanged = new();

        [JsonIgnore]
        public Uri GeoTiffFile
        {
            get => geoTiffFile;
            set
            {
                geoTiffFile = value;
                GeoTiffUriChanged.Invoke(value);
            }
        }

        public IEnumerable<LayerAsset> GetAssets()
        {
            if (geoTiffFile != null)
                yield return new LayerAsset(this, geoTiffFile);
        }
    }
}
