using System.Collections.Generic;
using Netherlands3D.Twin.Layers;

namespace Netherlands3D.Twin
{
    public class LocalFile
    {
        public string SourceUrl;
        public string LocalFilePath;

        public string OriginalFileName; // in order to avoid overwrites, we change the file name to a uuid, but we want to keep the original name for the user

        public List<string> log = new();
    }

    public interface IDataTypeAdapter
    {
        public bool Supports(LocalFile localFile);
        public void Execute(LocalFile localFile);
    }
}