using System.IO;
using Netherlands3D.Twin;
using UnityEngine;

namespace Netherlands3D.Functionalities.OBJImporter
{
    [CreateAssetMenu(menuName = "Netherlands3D/Adapters/OBJImportAdapter", fileName = "OBJImportAdapter", order = 0)]
    public class OBJImportAdapter : ScriptableObject, IDataTypeAdapter
    {
        [SerializeField] private OBJSpawner layerPrefab;

        public bool Supports(LocalFile localFile)
        {
            return localFile.LocalFilePath.EndsWith(".obj");
        }

        public void Execute(LocalFile localFile)
        {
            var fullPath = localFile.LocalFilePath;
            var fileName = Path.GetFileName(fullPath);
            OBJSpawner newLayer = Instantiate(layerPrefab);
            newLayer.gameObject.name = fileName;

            newLayer.SetObjPathInPropertyData(fullPath);
        }
    }
}