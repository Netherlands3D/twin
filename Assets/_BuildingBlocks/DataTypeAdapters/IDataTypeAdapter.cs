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
        public string FileName => Path.GetFileName(LocalFilePath);

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