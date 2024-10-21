using System.IO;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.Projects;
using UnityEngine;

namespace Netherlands3D.Twin
{
    [CreateAssetMenu(menuName = "Netherlands3D/Adapters/OBJImportAdapter", fileName = "OBJImportAdapter", order = 0)]
    public class ObjImportAdapter : ScriptableObject, IDataTypeAdapter
    {
        [SerializeField] private ObjSpawner layerPrefab;

        public bool Supports(LocalFile localFile)
        {
            return localFile.LocalFilePath.EndsWith(".obj");
        }

        public void Execute(LocalFile localFile)
        {
            var fullPath = localFile.LocalFilePath;
            ObjSpawner newLayer = Instantiate(layerPrefab);
            newLayer.gameObject.name = localFile.OriginalFileName;

            var propertyData = newLayer.PropertyData as ObjPropertyData;
            propertyData.ObjFile = AssetUriFactory.CreateProjectAssetUri(fullPath);
        }
    }
}