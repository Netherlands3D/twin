using System.IO;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.Projects;
using UnityEngine;

namespace Netherlands3D.Twin.Functionalities.GltfImporter
{
    [CreateAssetMenu(menuName = "Netherlands3D/Adapters/GltfImportAdapter", fileName = "GltfImportAdapter", order = 0)]
    public class GltfImportAdapter : ScriptableObject, IDataTypeAdapter
    {
        [SerializeField] private GltfSpawner layerPrefab;
        
        public bool Supports(LocalFile localFile)
        {
            return localFile.LocalFilePath.EndsWith(".glb") 
                || localFile.LocalFilePath.EndsWith(".gltf");
        }

        public void Execute(LocalFile localFile)
        {
            var fullPath = localFile.LocalFilePath;
            var fileName = Path.GetFileName(fullPath);
            GltfSpawner newLayer = Instantiate(layerPrefab);
            newLayer.gameObject.name = fileName;

            var propertyData = newLayer.PropertyData as GltfPropertyData;
            propertyData.Uri = AssetUriFactory.CreateProjectAssetUri(fullPath);
        }
    }
}