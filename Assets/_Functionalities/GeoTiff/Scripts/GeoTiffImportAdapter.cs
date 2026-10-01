using System;
using System.IO;
using Netherlands3D.DataTypeAdapters;
using Netherlands3D.Functionalities.GeoTiff.LayerPresets;
using Netherlands3D.Twin.Layers.LayerPresets;
using Netherlands3D.Twin.Projects;
using UnityEngine;

namespace Netherlands3D.Functionalities.GeoTiff
{
    [CreateAssetMenu(menuName = "Netherlands3D/Adapters/GeoTIFFImportAdapter", fileName = "GeoTIFFImportAdapter", order = 0)]
    public class GeoTiffImportAdapter : ScriptableObject, IDataTypeAdapter<LayerPresetArgs>
    {
        public bool Supports(LocalFile localFile)
        {
            if (localFile == null || string.IsNullOrEmpty(localFile.LocalFilePath) || !File.Exists(localFile.LocalFilePath))
                return false;
            var extension = Path.GetExtension(localFile.LocalFilePath);
            if (!extension.Equals(".tif", StringComparison.OrdinalIgnoreCase) &&
                !extension.Equals(".tiff", StringComparison.OrdinalIgnoreCase))
                return false;

            try
            {
                using var stream = new FileStream(localFile.LocalFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                var header = new byte[4];
                if (stream.Read(header, 0, header.Length) != header.Length)
                    return false;
                return (header[0] == 'I' && header[1] == 'I' && (header[2] == 42 || header[2] == 43) && header[3] == 0) ||
                       (header[0] == 'M' && header[1] == 'M' && header[2] == 0 && (header[3] == 42 || header[3] == 43));
            }
            catch
            {
                return false;
            }
        }

        public LayerPresetArgs Execute(LocalFile localFile)
        {
            return new GeoTiffPreset.Args(localFile.FileName, AssetUriFactory.ConvertLocalFileToAssetUri(localFile));
        }
    }
}
