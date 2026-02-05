using Netherlands3D.DataTypeAdapters;
using Netherlands3D.Functionalities.OBJImporter.LayerPresets;
using Netherlands3D.Twin;
using Netherlands3D.Twin.Projects;
using Netherlands3D.Twin.Services;
using UnityEngine;

namespace Netherlands3D.Functionalities.OBJImporter
{
    [CreateAssetMenu(menuName = "Netherlands3D/Adapters/OBJImportAdapter", fileName = "OBJImportAdapter", order = 0)]
    public class OBJImportAdapter : ScriptableObject, IDataTypeAdapter<Layer>
    {
        private const string SupportedFileExtension = "obj";

        public bool Supports(LocalFile localFile) => localFile.LocalFilePath.EndsWith($".{SupportedFileExtension}");

        public Layer Execute(LocalFile localFile)
        {
            var uri = AssetUriFactory.ConvertLocalFileToAssetUri(localFile);

            return App.Layers.Add(new ObjPreset.Args(localFile.FileName, uri));
        }
    }
}