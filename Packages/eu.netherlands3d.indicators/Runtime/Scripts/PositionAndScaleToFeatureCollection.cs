using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GeoJSON.Net.CoordinateReferenceSystem;
using GeoJSON.Net.Feature;
using GeoJSON.Net.Geometry;
using Netherlands.Indicators.ExtensionMethods;
using Netherlands3D.Coordinates;
using Netherlands3D.SelectionTools;
using Netherlands3D.TileSystem;
using UnityEngine;

namespace Netherlands.Indicators
{
    /// <summary>
    /// Positions and scales the current game object to match the total area of the given feature collection.
    ///
    /// This component assumes that the attached mesh/sprite equals 1 square unity unit/meter; similar to a regular
    /// 3D Plane.
    /// </summary>
    public class PositionAndScaleToFeatureCollection : MonoBehaviour
    {
        void Start()
        {
            Hide();
        }

        public void Show(FeatureCollection featureCollection)
        {
            if (featureCollection == null)
            {
                Hide();
                return;
            }

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

            Debug.Log(topLeft.ToVector3());
            Debug.Log(bottomRight.ToVector3());

            Vector3 size = new Vector3(
                bottomRight.ToVector3().x - topLeft.ToVector3().x,
                0, 
                bottomRight.ToVector3().z - topLeft.ToVector3().z
            );

            transform.position = new Vector3(center.ToVector3().x, transform.position.y, center.ToVector3().z);
            transform.localScale = new Vector3(size.x, size.y, size.z);
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            transform.position = new Vector3(0, transform.position.y, 0);
            transform.localScale = Vector3.one;

            gameObject.SetActive(false);
        }
    }
}
