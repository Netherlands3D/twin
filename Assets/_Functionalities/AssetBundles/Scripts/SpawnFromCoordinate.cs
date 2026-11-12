using Netherlands3D.Coordinates;
using UnityEngine;

namespace Netherlands3D
{
    public class SpawnFromCoordinate : MonoBehaviour
    {
        public CoordinateSystem CoordinateSystem => coordinateSystem;
        public Coordinate Coordinate =>  new Coordinate(coordinateSystem, Coordinates.y, Coordinates.x, Coordinates.z);
        
        [SerializeField] private Vector3 Coordinates = Vector3.zero;
        [SerializeField] private CoordinateSystem coordinateSystem;
    }
}
