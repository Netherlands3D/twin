namespace Netherlands3D.Twin
{
    public class LocalFile
    {
        public string SourceUrl;
        public string LocalFilePath;
    }

    public interface IDataTypeAdapter
    {
        public bool Supports(LocalFile localFile);
        public void Execute(LocalFile localFile);
    }
}