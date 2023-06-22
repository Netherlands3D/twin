using GeoJSON.Net.Feature;
using Netherlands.Indicators.ExtensionMethods;
using Netherlands3D.Coordinates;
using UnityEngine;
using UnityEngine.Events;

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
        public UnityEvent<Vector3> onSetPosition = new();
        public UnityEvent<Vector3> onSetSize = new();
        
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

            Vector3 position = new Vector3(
                center.ToVector3().x, 
                transform.position.y, 
                center.ToVector3().z
            );
            Vector3 scale = new Vector3(
                bottomRight.ToVector3().x - topLeft.ToVector3().x,
                0, 
                bottomRight.ToVector3().z - topLeft.ToVector3().z
            );
            // Size is expressed in Width x Height x Depth, thus: x, z, y
            Vector3 size = new Vector3(scale.x, scale.z, scale.y);

            transform.position = position;
            onSetPosition.Invoke(position);
            transform.localScale = scale;
            onSetSize.Invoke(size); 
            
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
