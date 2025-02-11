using Netherlands3D.Twin.Utility;

namespace Netherlands3D.Functionalities.ObjectInformation
{
    public interface IMapping 
    {
        public object MappingObject { get; }
        public BoundingBox BoundingBox { get; }
    }
}
