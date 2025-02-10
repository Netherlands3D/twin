using Netherlands3D.Coordinates;
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
        public ObjectMapping ObjectMapping => objectMapping;
        public BoundingBox BoundingBox => boundingBox;

        private ObjectMapping objectMapping;
        private BoundingBox boundingBox;
        private MeshRenderer meshRenderer;

        public void SetMeshObject(ObjectMapping mapping)
        {
            this.objectMapping = mapping;
            meshRenderer = this.objectMapping.GetComponent<MeshRenderer>();
        }

        //maybe this should be automated and called within the set visualisation layer
        public void UpdateBoundingBox()
        {
            if (ObjectMapping == null)
            {
                Debug.LogError("must have feature for boundingbox");
                return;
            }
            if (meshRenderer == null)
            {
                Debug.LogError("must have a renderer to determine the boundingbox");
                return;
            }
            boundingBox = CreateBoundingBoxForMesh(ObjectMapping, meshRenderer);
        }

        public static BoundingBox CreateBoundingBoxForMesh(ObjectMapping mapping, MeshRenderer renderer)
        {
            Bounds featureBounds = renderer.bounds;
            Coordinate bottomLeft = new Coordinate(CoordinateSystem.Unity, featureBounds.min.x, featureBounds.min.y, featureBounds.min.z);
            Coordinate topRight = new Coordinate(CoordinateSystem.Unity, featureBounds.max.x, featureBounds.max.y, featureBounds.max.z);
            Coordinate blWgs84 = bottomLeft.Convert(CoordinateSystem.WGS84_LatLon);
            Coordinate trWgs84 = topRight.Convert(CoordinateSystem.WGS84_LatLon);
            BoundingBox boundingBox = new(blWgs84, trWgs84);
            return boundingBox;
        }
    }
}