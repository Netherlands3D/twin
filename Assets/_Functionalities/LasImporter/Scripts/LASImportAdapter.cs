using Netherlands3D.DataTypeAdapters;
using Netherlands3D.Functionalities.LASImporter.LayerPresets;
using Netherlands3D.Twin;
using Netherlands3D.Twin.Projects;
using UnityEngine;

namespace Netherlands3D.Functionalities.LASImporter
{
    [CreateAssetMenu(menuName = "Netherlands3D/Adapters/LASImportAdapter", fileName = "LASImportAdapter", order = 0)]
    public class LASImportAdapter : ScriptableObject, IDataTypeAdapter
    {
        private const string SupportedFileExtension = "las";

        public bool Supports(LocalFile localFile)
            => localFile.LocalFilePath.EndsWith($".{SupportedFileExtension}");

        public void Execute(LocalFile localFile)
        {
            // your filebrowser already turned this into a LocalFile
            // we turn it into a project asset uri, same as OBJ does
            var uri = AssetUriFactory.ConvertLocalFileToAssetUri(localFile);

            // tell the layer system to spawn a LAS layer
            App.Layers.Add(new LasPreset.Args(localFile.FileName, uri));
        }
    }
}
