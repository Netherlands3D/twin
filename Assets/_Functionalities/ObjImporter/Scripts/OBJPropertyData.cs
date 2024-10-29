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
        [DataMember] private Uri[] textureFiles = Array.Empty<Uri>();

        [JsonIgnore] public readonly UnityEvent<Uri> OnObjDataChanged = new();
        [JsonIgnore] public readonly UnityEvent<Uri> OnMtlDataChanged = new();
        [JsonIgnore] public readonly UnityEvent<Uri[]> OnTextureFilesDataChanged = new();

        [JsonIgnore]
        public Uri ObjFile
        {
            get => objFile;
            set
            {
                objFile = value;
                OnObjDataChanged.Invoke(value);
            }
        }
        
        [JsonIgnore]
        public Uri MtlFile
        {
            get => mtlFile;
            set
            {
                mtlFile = value;
                OnMtlDataChanged.Invoke(value);
            }
        }
        
        [JsonIgnore]
        public Uri[] TextureFiles
        {
            get => textureFiles;
            set
            {
                textureFiles = value;
                OnTextureFilesDataChanged.Invoke(textureFiles);
            }
        }

        public IEnumerable<LayerAsset> GetAssets()
        {
            var assetList = new List<LayerAsset>();
            
            assetList.Add(new (this, objFile != null ? objFile : null));
            assetList.Add(new (this, mtlFile != null ? mtlFile : null));

            foreach (var textureFile in textureFiles)
            {
                assetList.Add(new (this, textureFile != null ? textureFile : null));
            }

            return assetList;
        }
    }
}
