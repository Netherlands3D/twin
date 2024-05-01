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
    public class PolygonShifter : MonoBehaviour
    {
        private WorldTransform worldTransform;
        private PolygonVisualisation polygonVisualisation;
        private List<List<Coordinate>> preshiftPolygonsCoordinates;
        private List<List<Vector3>> currentPolygons;

        void Awake()
        {
            polygonVisualisation = GetComponent<PolygonVisualisation>();

            worldTransform = gameObject.AddComponent<WorldTransform>();
            worldTransform.onPreShift.AddListener(StoreLists);
            worldTransform.onPostShift.AddListener(ReapplyLists);
        }

        private void StoreLists(WorldTransform worldTransform, Coordinate coordinate)
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
                    var unityCoordinate = new Coordinate(CoordinateSystem.Unity, point.x, point.y, point.z);
                    var worldCoordinate = CoordinateConverter.ConvertTo(unityCoordinate, CoordinateSystem.WGS84);
                    preshiftPolygonsCoordinates[i].Add(worldCoordinate);
                }
            }
        }

        private void ReapplyLists(WorldTransform worldTransform, Coordinate coordinate)
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
