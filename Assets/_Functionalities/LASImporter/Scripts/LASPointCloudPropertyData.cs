using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Netherlands3D.Twin.Layers.Properties;
using Newtonsoft.Json;
using UnityEngine.Events;

namespace Netherlands3D.Functionalities.LASImporter
{
    [DataContract(Namespace = "https://netherlands3d.eu/schemas/projects/layers/properties", Name = "LasPointCloud")]
    public class LASPointCloudPropertyData : LayerPropertyData, ILayerPropertyDataWithAssets
    {
        [DataMember] private Uri lasFile;

        [JsonIgnore] public readonly UnityEvent<Uri> LasUriChanged = new();

        [JsonIgnore]
        public Uri LasFile
        {
            get => lasFile;
            set
            {
                lasFile = value;
                LasUriChanged.Invoke(value);
            }
        }

        public IEnumerable<LayerAsset> GetAssets()
        {
            if (lasFile != null)
            {
                yield return new LayerAsset(this, lasFile);
            }
        }
    }
}
