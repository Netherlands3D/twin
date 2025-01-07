using System.Collections;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;
using Newtonsoft.Json;
using UnityEngine.Events;

namespace Netherlands3D.Twin.Layers.Properties
{
    [DataContract(Namespace = "https://netherlands3d.eu/schemas/projects/layers/properties", Name = "Obj")]
    public class ObjPropertyData : LayerPropertyData, ILayerPropertyDataWithAssets
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