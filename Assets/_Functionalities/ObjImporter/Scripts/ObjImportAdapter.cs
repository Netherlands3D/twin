using System.IO;
using Netherlands3D.Twin;
using UnityEngine;

namespace Netherlands3D.Functionalities.ObjImporter
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
            var fileName = Path.GetFileName(fullPath);
            ObjSpawner newLayer = Instantiate(layerPrefab);
            newLayer.gameObject.name = fileName;

            newLayer.SetObjPathInPropertyData(fullPath);
        }
    }
}