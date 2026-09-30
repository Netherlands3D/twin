using System;
using System.Collections.Generic;
using System.IO;
using Netherlands3D.Twin.Services;

namespace Netherlands3D.DataTypeAdapters
{
    public class LocalFile
    {
        public string SourceUrl;
        public string LocalFilePath;
        public string FileName
        {
            get
            {
                if (!string.IsNullOrEmpty(LocalFilePath))
                    return Path.GetFileName(LocalFilePath);

                return Uri.TryCreate(SourceUrl, UriKind.Absolute, out var uri)
                    ? Uri.UnescapeDataString(Path.GetFileName(uri.AbsolutePath))
                    : Path.GetFileName(SourceUrl);
            }
        }

        public List<string> log = new();
    }

    public interface IDataTypeAdapter<out T>
    {
        public bool Supports(LocalFile localFile);
        public T Execute(LocalFile localFile);
    }

    public class AdapterNotFoundException : Exception
    {
        public AdapterNotFoundException(string message) : base(message) { }
    }
}
