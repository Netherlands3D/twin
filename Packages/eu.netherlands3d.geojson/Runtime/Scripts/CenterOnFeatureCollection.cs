using System;
using GeoJSON.Net.Feature;
using Netherlands3D.Coordinates;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands.GeoJSON
{
    /// <summary>
    /// Positions a game object to match the center of the given feature collection.
    ///
    /// This component assumes that the attached mesh/sprite equals 1 square unity unit/meter; similar to a regular
    /// 3D Plane.
    /// </summary>
    public class CenterOnFeatureCollection : MonoBehaviour
    {
        [SerializeField] private GameObject targetGameObject;

        public UnityEvent<Vector3> onSetPosition = new();

        private void Awake()
        {
            if (targetGameObject == null)
            {
                targetGameObject = gameObject;
            }
        }

        public void CenterOn(FeatureCollection featureCollection)
        {
            double[] boundingBox = featureCollection.BoundingBoxes ?? featureCollection.DerivedBoundingBoxes();
            int epsgId = featureCollection.EPSGId();

            Coordinate topLeft = CoordinateConverter.ConvertTo(
                new Coordinate(epsgId, boundingBox[0], boundingBox[1], 0),
                CoordinateSystem.Unity
            );
            Coordinate bottomRight = CoordinateConverter.ConvertTo(
                new Coordinate(epsgId, boundingBox[2], boundingBox[3], 0),
                CoordinateSystem.Unity
            );
            Coordinate center = CoordinateConverter.ConvertTo(
                new Coordinate(
                    epsgId, 
                    (boundingBox[0] + boundingBox[2]) / 2d, 
                    (boundingBox[1] + boundingBox[3]) / 2d, 
                    0
                ),
                CoordinateSystem.Unity
            );

            Vector3 position = new Vector3(
                center.ToVector3().x, 
                transform.position.y, 
                center.ToVector3().z
            );

            transform.position = position;
            onSetPosition.Invoke(position);
        }
    }
}