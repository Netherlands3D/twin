using Netherlands3D.SubObjects;
using Netherlands3D.Twin.Utility;
using UnityEngine;

namespace Netherlands3D.Functionalities.ObjectInformation 
{
    /// <summary>
    /// in the future/when production ready, this should probably be renamed to a feature mapping type, as it contains geometry and a boundingbox
    /// </summary>
    public class MeshMapping : MonoBehaviour, IMapping
    {
        public ObjectMapping ObjectMapping;
        public BoundingBox BoundingBox => boundingBox;

        private ObjectMapping objectMapping;
        private BoundingBox boundingBox;

        public void SetFeature(ObjectMapping mapping)
        {
            this.objectMapping = mapping;
        }
    }
}