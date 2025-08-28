using Netherlands3D.DataTypeAdapters;
using Netherlands3D.Functionalities.OBJImporter.LayerPresets;
using Netherlands3D.Twin;
using Netherlands3D.Twin.Projects;
using UnityEngine;

namespace Netherlands3D.Functionalities.OBJImporter
{
    [CreateAssetMenu(menuName = "Netherlands3D/Adapters/OBJImportAdapter", fileName = "OBJImportAdapter", order = 0)]
    public class OBJImportAdapter : ScriptableObject, IDataTypeAdapter
    {
        public bool Supports(LocalFile localFile) => localFile.LocalFilePath.EndsWith(".obj");

        public void Execute(LocalFile localFile)
        {
            var objUri = AssetUriFactory.ConvertLocalFileToAssetUri(localFile);

            App.Layers.Add(
                "obj", 
                new Obj.Args(localFile.FileName, objUri)
            );
        }
    }
}