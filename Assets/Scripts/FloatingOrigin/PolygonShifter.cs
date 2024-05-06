using System;
using System.Collections;
using System.Collections.Generic;
using Netherlands3D.Coordinates;
using Netherlands3D.SelectionTools;
using Netherlands3D.Twin.FloatingOrigin;
using UnityEngine;

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
        private List<List<Vector3>> currentPolygons;

        private Coordinate beforeShiftCoordinate;

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
            currentPolygons = polygonVisualisation.Polygons;

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
                    var worldCoordinate = CoordinateConverter.ConvertTo(unityCoordinate, CoordinateSystem.RD);
                    preshiftPolygonsCoordinates[i].Add(worldCoordinate);
                }
            }
        }

        private void ConvertAndApplyCoordinates()
        {
            //Update currentPolygons
            for (int i = 0; i < preshiftPolygonsCoordinates.Count; i++)
            {
                for (int j = 0; j < preshiftPolygonsCoordinates[i].Count; j++)
                {
                    var worldCoordinate = preshiftPolygonsCoordinates[i][j];
                    var unityCoordinate = CoordinateConverter.ConvertTo(worldCoordinate, CoordinateSystem.Unity);
                    currentPolygons[i][j] = new Vector3((float)unityCoordinate.Points[0], (float)unityCoordinate.Points[1], (float)unityCoordinate.Points[2]);
                }
            }

            //Trigger reapplied points
            polygonVisualisation.UpdateVisualisation(currentPolygons);
        }
    }
}
