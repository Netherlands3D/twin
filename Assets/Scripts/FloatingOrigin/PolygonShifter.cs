using System;
using System.Collections;
using System.Collections.Generic;
using Netherlands3D.Coordinates;
using Netherlands3D.SelectionTools;
using Netherlands3D.Twin.FloatingOrigin;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin
{
    /// <summary>
    /// This class is responsible for shifting the all the polygons their points when the world origin is shifted.
    /// The points before shift are stored as world coordinates and reapplyed as unity coordinates after the shift.
    /// </summary>
    public class PolygonShifter : WorldTransformShifter
    {
        private PolygonVisualisation polygonVisualisation;
        private List<List<Coordinate>> preshiftPolygonsCoordinates;
        private Coordinate beforeShiftCoordinate;

        public UnityEvent<List<List<Vector3>>> polygonShifted = new();

        void Awake()
        {
            polygonVisualisation = GetComponent<PolygonVisualisation>();
        }
        public override void PrepareToShift(WorldTransform worldTransform, Coordinate from, Coordinate to)
        {
            var unityCoordinate = new Coordinate(CoordinateSystem.Unity, transform.position.x, transform.position.y, transform.position.z);
            beforeShiftCoordinate = CoordinateConverter.ConvertTo(unityCoordinate,CoordinateSystem.RD);

            StoreLocalUnityCoordinatesLists();
        }

        public override void ShiftTo(WorldTransform worldTransform, Coordinate from, Coordinate to)
        {
            var newUnityCoordinate = CoordinateConverter.ConvertTo(beforeShiftCoordinate, CoordinateSystem.Unity);
            Debug.Log("Shifted from " + beforeShiftCoordinate + " to " + newUnityCoordinate);
            ConvertAndApplyCoordinates();
        }

        private void StoreLocalUnityCoordinatesLists()
        {
            var currentPolygons = polygonVisualisation.Polygons;

            //Create matching points list with world coordinates
            preshiftPolygonsCoordinates = new List<List<Coordinate>>(currentPolygons.Count);
            for (int i = 0; i < currentPolygons.Count; i++)
            {
                preshiftPolygonsCoordinates.Add(new List<Coordinate>()); // Add a new list for each polygon

                for (int j = 0; j < currentPolygons[i].Count; j++)
                {
                    var point = currentPolygons[i][j];
                    var unityCoordinate = new Coordinate(
                        CoordinateSystem.Unity, 
                        point.x, 
                        point.y, 
                        point.z
                    );
                    var worldCoordinate = CoordinateConverter.ConvertTo(unityCoordinate, CoordinateSystem.WGS84);
                    preshiftPolygonsCoordinates[i].Add(worldCoordinate);
                }
            }

            Debug.Log("Second point before shift: " + currentPolygons[0][1]);
        }

        private void ConvertAndApplyCoordinates()
        {
            // Initialize newPolygons list
            var newPolygons = new List<List<Vector3>>(preshiftPolygonsCoordinates.Count);
            for (int i = 0; i < preshiftPolygonsCoordinates.Count; i++)
            {
                newPolygons.Add(new List<Vector3>());
            }

            // Update currentPolygons
            for (int i = 0; i < preshiftPolygonsCoordinates.Count; i++)
            {
                for (int j = 0; j < preshiftPolygonsCoordinates[i].Count; j++)
                {
                    var worldCoordinate = preshiftPolygonsCoordinates[i][j];
                    var unityCoordinate = CoordinateConverter.ConvertTo(worldCoordinate, CoordinateSystem.Unity);
                    var unityVector3Coordinate = new Vector3((float)unityCoordinate.Points[0], (float)unityCoordinate.Points[1], (float)unityCoordinate.Points[2]);
                    newPolygons[i].Add(unityVector3Coordinate);
                }
            }

            Debug.Log("Second point after shift: " + newPolygons[0][1]);

            // Trigger reapplied points
            polygonVisualisation.UpdateVisualisation(newPolygons);
            polygonShifted.Invoke(newPolygons);
        }
    }
}
