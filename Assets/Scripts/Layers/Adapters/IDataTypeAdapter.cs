using System.Collections.Generic;
using Netherlands3D.Twin.Layers;

namespace Netherlands3D.Twin
{
    public class LocalFile
    {
        public string SourceUrl;
        public string LocalFilePath;

        public List<string> log = new();
    }

    public interface IDataTypeAdapter
    {
        public bool Supports(LocalFile localFile);
        public void Execute(LocalFile localFile);
    }
}