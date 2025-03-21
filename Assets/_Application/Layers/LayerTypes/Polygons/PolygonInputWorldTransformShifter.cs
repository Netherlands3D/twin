using System.Collections.Generic;
using Netherlands3D.Coordinates;
using Netherlands3D.SelectionTools;
using Netherlands3D.Twin.FloatingOrigin;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin.Layers.LayerTypes.Polygons
{
    /// <summary>
    /// This class is responsible for shifting the all the polygons their point individualy when the world origin is shifted.
    /// The points before shift are stored as world coordinates and reapplyed as unity coordinates after the shift.
    /// </summary>
    public class PolygonInputWorldTransformShifter : WorldTransformShifter
    {
        public PolygonInput polygonInput;
        private List<Coordinate> preshiftPolygonsCoordinates;
        public UnityEvent<List<Vector3>> polygonShifted = new();

        public override void PrepareToShift(WorldTransform worldTransform, Coordinate fromOrigin, Coordinate toOrigin)
        {
            StoreLocalUnityCoordinatesLists();
        }

        public override void ShiftTo(WorldTransform worldTransform, Coordinate fromOrigin, Coordinate toOrigin)
        {
            ConvertAndApplyCoordinates();
        }

        private void StoreLocalUnityCoordinatesLists()
        {
            //Create matching points list with world coordinates
            var positions = polygonInput.positions;
            preshiftPolygonsCoordinates = new List<Coordinate>(positions.Count);
            for (int i = 0; i < positions.Count; i++)
            {
                var point = positions[i];
                var worldCoordinate = new Coordinate(point);
                preshiftPolygonsCoordinates.Add(worldCoordinate);
            }
        }

        private void ConvertAndApplyCoordinates()
        {
            // Initialize newPolygons list
            var newPolygon = new List<Vector3>(preshiftPolygonsCoordinates.Count);

            // Update currentPolygons
            for (int i = 0; i < preshiftPolygonsCoordinates.Count; i++)
            {
                var worldCoordinate = preshiftPolygonsCoordinates[i];
                var unityCoordinate = worldCoordinate.ToUnity();
                newPolygon.Add(unityCoordinate);
            }

            // Trigger reapplied points
            polygonShifted.Invoke(newPolygon);
        }
    }
}
