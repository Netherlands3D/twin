using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Coordinates
{
    public static class MovingOrigin
    {
        public static UnityEvent prepareForOriginShift = new();
        public class CenterChangedEvent : UnityEvent<Vector3> { }
        public static CenterChangedEvent relativeOriginChanged = new();

        public static void MoveAndRotateWorld(Vector3 cameraPosition)
        {
            prepareForOriginShift.Invoke();

            Vector3 pos = new Vector3(cameraPosition.x, 0, cameraPosition.z);
            var flatCameraPosition = new Coordinate(pos);
            var wgsCoordinate = CoordinateConverter.ConvertTo(flatCameraPosition, CoordinateSystem.WGS84_LatLonHeight);
            EPSG4936.relativeCenter = CoordinateConverter.ConvertTo(wgsCoordinate, CoordinateSystem.ETRS89_ECEF).ToVector3ECEF();

            var offset = new Vector3(-cameraPosition.x, 0, -cameraPosition.z);

            relativeOriginChanged.Invoke(offset);
        }

    }
}
