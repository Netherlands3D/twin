using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.Properties;

namespace Netherlands3D.Functionalities.LASImporter
{
    [DataContract(Namespace = "https://netherlands3d.eu/schemas/projects/layers/properties", Name = "Las")]
    public class LASPropertyData : LayerPropertyData, ILayerPropertyDataWithAssets
    {
        [DataMember] private Uri lasFile;

        public Uri LasFile
        {
            get => lasFile;
            set => lasFile = value;
        }

        public IEnumerable<LayerAsset> GetAssets()
        {
            var assets = new List<LayerAsset>();
            if (lasFile != null)
                assets.Add(new LayerAsset(this, lasFile));
            return assets;
        }
    }
}
