using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Netherlands3D.Twin.Projects;

namespace Netherlands3D.Twin.Layers.Properties
{
    [DataContract(Namespace = "https://netherlands3d.eu/schemas/projects/layers/properties", Name = "Url")]
    public class LayerURLPropertyData : LayerPropertyData, ILayerPropertyDataWithAssets
    {
        [DataMember] public string url = "";

        public IEnumerable<LayerAsset> GetAssets()
        {
            Uri uri = null;
            if (!string.IsNullOrEmpty(url))
                uri = AssetUriFactory.CreateProjectAssetUri(url);

            return new List<LayerAsset>()
            {
                new(this, uri)
            };
        }
    }
}