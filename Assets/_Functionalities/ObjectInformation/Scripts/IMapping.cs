using Netherlands3D.Twin.Utility;
using UnityEngine;

namespace Netherlands3D.Functionalities.ObjectInformation
{
    public interface IMapping 
    {
        public string Id { get; }
        public object MappingObject { get; }
        public BoundingBox BoundingBox { get; }
        
        public GameObject Select(string subId = null);
        public void Deselect();
    }
}
