using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using UnityEngine.Events;

namespace Netherlands3D.Twin.Layers.Properties
{
    public class OBJPropertyData : LayerPropertyData, ILayerPropertyDataWithAssets
    {
        [SerializeField, JsonProperty] private Uri objFile;

        [JsonIgnore] public readonly UnityEvent<Uri> OnDataChanged = new();

        [JsonIgnore]
        public Uri ObjFile
        {
            get => objFile;
            set
            {
                objFile = value;
                OnDataChanged.Invoke(value);
            }
        }

        public IEnumerable<LayerAsset> GetAssets()
        {
            return new List<LayerAsset>()
            {
                new (this, objFile != null ? objFile : null)
            };
        }
    }
}
