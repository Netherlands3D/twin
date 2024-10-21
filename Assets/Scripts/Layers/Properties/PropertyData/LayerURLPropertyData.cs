using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Netherlands3D.Twin.Projects;
using UnityEngine;

namespace Netherlands3D.Twin.Layers.Properties
{
    [DataContract(Namespace = "https://netherlands3d.eu/schemas/projects/layers/properties", Name = "Url")]
    public class LayerURLPropertyData : LayerPropertyData, ILayerPropertyDataWithAssets
    {
        [DataMember] public Uri uri;

        public IEnumerable<LayerAsset> GetAssets()
        {
            return new List<LayerAsset>()
            {
                new(this, uri)
            };
        }
    }
}