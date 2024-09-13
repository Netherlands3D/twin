using System.Collections;
using System.Collections.Generic;
using System.IO;
using Netherlands3D.Twin.FloatingOrigin;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.Projects;
using Netherlands3D.Twin.UI.LayerInspector;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin
{
    [CreateAssetMenu(menuName = "Netherlands3D/Adapters/OBJImportAdapter", fileName = "OBJImportAdapter", order = 0)]
    public class ObjImportAdapter : ScriptableObject, IDataTypeAdapter
    {
        [SerializeField] private OBJLayerGameObject layerPrefab;


        public bool Supports(LocalFile localFile)
        {
            var hasObjExtention = localFile.LocalFilePath.EndsWith(".obj");
            if (hasObjExtention)
            {
               
                Debug.Log("has obj-extention");
                return true;
            }
            Debug.Log("has no obj-extention");
            return false;
        }

        public void Execute(LocalFile localFile)
        {
            var fullPath = localFile.LocalFilePath;
            var fileName = Path.GetFileName(fullPath);

            OBJLayerGameObject newLayer = Instantiate(layerPrefab);
            newLayer.gameObject.name = fileName;

            var propertyData = newLayer.PropertyData as OBJPropertyData;
            propertyData.Data = AssetUriFactory.CreateProjectAssetUri(fullPath);
            
        }

    }
}
