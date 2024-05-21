using System;
using System.Collections;
using System.Collections.Generic;
using Netherlands3D.Coordinates;
using Netherlands3D.SelectionTools;
using Netherlands3D.Twin.FloatingOrigin;
using Netherlands3D.Twin.Layers;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin
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
            var unityCoordinate = new Coordinate(CoordinateSystem.Unity, transform.position.x, transform.position.y, transform.position.z);
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
                var unityCoordinate = new Coordinate(
                    CoordinateSystem.Unity, 
                    point.x, 
                    point.y, 
                    point.z
                );
                var worldCoordinate = CoordinateConverter.ConvertTo(unityCoordinate, CoordinateSystem.WGS84);
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
                var unityCoordinate = CoordinateConverter.ConvertTo(worldCoordinate, CoordinateSystem.Unity);
                var unityVector3Coordinate = new Vector3((float)unityCoordinate.Points[0], (float)unityCoordinate.Points[1], (float)unityCoordinate.Points[2]);
                newPolygon.Add(unityVector3Coordinate);
            }

            // Trigger reapplied points
            polygonShifted.Invoke(newPolygon);
        }
    }
}
