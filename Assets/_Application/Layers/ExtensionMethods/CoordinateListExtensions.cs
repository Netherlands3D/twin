using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Coordinates;
using UnityEngine;

namespace Netherlands3D.Twin.Layers.ExtensionMethods
{
    public static class CoordinateListExtensions
    {
        public static IEnumerable<Coordinate> ToCoordinates(this IEnumerable<Vector3> positions)
        {
            return positions.Select(point => new Coordinate(point));
        }
        
        public static IEnumerable<Vector3> ToUnityPositions(this IEnumerable<Coordinate> coordinates)
        {
            return coordinates.Select(coordinate => coordinate.ToUnity());
        }
    }
}