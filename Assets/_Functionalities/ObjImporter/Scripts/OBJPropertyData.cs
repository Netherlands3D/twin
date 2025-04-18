using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.Properties;
using Newtonsoft.Json;
using UnityEngine.Events;

namespace Netherlands3D.Functionalities.OBJImporter
{
    [DataContract(Namespace = "https://netherlands3d.eu/schemas/projects/layers/properties", Name = "Obj")]
    public class OBJPropertyData : LayerPropertyData, ILayerPropertyDataWithAssets
    {
        [DataMember] private Uri objFile;
        [DataMember] private Uri mtlFile;

        [JsonIgnore] public readonly UnityEvent<Uri> OnObjUriChanged = new();
        [JsonIgnore] public readonly UnityEvent<Uri> OnMtlUriChanged = new();

        [JsonIgnore]
        public Uri ObjFile
        {
            get => objFile;
            set
            {
                objFile = value;
                OnObjUriChanged.Invoke(value);
            }
        }

        [JsonIgnore]
        public Uri MtlFile
        {
            get => mtlFile;
            set
            {
                mtlFile = value;
                OnMtlUriChanged.Invoke(value);
            }
        }

        public IEnumerable<LayerAsset> GetAssets()
        {
            var existingAssets = new List<LayerAsset>();

            if (objFile != null)
            {
                existingAssets.Add(new(this, objFile));
            }

            if (mtlFile != null)
            {
                existingAssets.Add(new(this, mtlFile));
            }

            return existingAssets;
        }
    }
}