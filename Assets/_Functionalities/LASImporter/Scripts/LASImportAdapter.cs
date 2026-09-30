using System;
using System.IO;
using Netherlands3D.DataTypeAdapters;
using Netherlands3D.Functionalities.LASImporter.LayerPresets;
using Netherlands3D.Twin.Layers.LayerPresets;
using Netherlands3D.Twin.Projects;
using UnityEngine;

namespace Netherlands3D.Functionalities.LASImporter
{
    [CreateAssetMenu(menuName = "Netherlands3D/Adapters/LASImportAdapter", fileName = "LASImportAdapter", order = 0)]
    public class LASImportAdapter : ScriptableObject, IDataTypeAdapter<LayerPresetArgs>
    {
        public bool Supports(LocalFile localFile)
        {
            if (localFile == null)
                return false;

            if (Uri.TryCreate(localFile.SourceUrl, UriKind.Absolute, out var sourceUri)
                && sourceUri.Scheme.Equals("browser-file", StringComparison.OrdinalIgnoreCase))
            {
                return Path.GetExtension(sourceUri.AbsolutePath).Equals(".las", StringComparison.OrdinalIgnoreCase);
            }

            if (string.IsNullOrEmpty(localFile.LocalFilePath) || !File.Exists(localFile.LocalFilePath))
                return false;

            try
            {
                using var fs = new FileStream(localFile.LocalFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                if (fs.Length < 4)
                    return false;

                var header = new byte[4];
                return fs.Read(header, 0, header.Length) == 4
                    && header[0] == 'L'
                    && header[1] == 'A'
                    && header[2] == 'S'
                    && header[3] == 'F';
            }
            catch
            {
                return false;
            }
        }

        public LayerPresetArgs Execute(LocalFile localFile)
        {
            var uri = Uri.TryCreate(localFile.SourceUrl, UriKind.Absolute, out var sourceUri)
                      && sourceUri.Scheme.Equals("browser-file", StringComparison.OrdinalIgnoreCase)
                ? sourceUri
                : AssetUriFactory.ConvertLocalFileToAssetUri(localFile);
            return new LASPointCloudPreset.Args(localFile.FileName, uri);
        }
    }
}
