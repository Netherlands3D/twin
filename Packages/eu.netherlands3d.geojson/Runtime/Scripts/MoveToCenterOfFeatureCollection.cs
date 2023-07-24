using System;
using GeoJSON.Net.Feature;
using Netherlands3D.Coordinates;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace Netherlands.GeoJSON
{
    /// <summary>
    /// Positions a game object to match the center of the given feature collection.
    ///
    /// This component assumes that the attached mesh/sprite equals 1 square unity unit/meter; similar to a regular
    /// 3D Plane.
    /// </summary>
    public class MoveToCenterOfFeatureCollection : MonoBehaviour
    {
        private readonly float elevation = 0f;

        [Tooltip("The target game object to move.")]
        [SerializeField] private GameObject targetGameObject;
        
        [Tooltip("By default, the Y position of the target Game Object is maintained; when this toggle is enabled the game object is instead pulled back on its local Z-axis")]
        [SerializeField] private bool pullBack;

        [SerializeField] private float pullBackFactor = 1.0f;

        [Tooltip("The amount of time it should take to animate to the desired position")]
        [SerializeField] private float movingDuration = 1.0f;

        public UnityEvent<Vector3> onPositionChanged = new();

        private void Awake()
        {
            if (targetGameObject == null)
            {
                targetGameObject = gameObject;
            }
        }

        public void CenterOn(FeatureCollection featureCollection)
        {
            CalculateCenterAndExtents(featureCollection, out var position, out var extents);

            Debug.Log($"Moving {targetGameObject} to {position}");
            var targetTransform = targetGameObject.transform;

            Move(targetTransform, PullbackPosition(targetTransform, position, extents));
        }

        private Vector3 PullbackPosition(Transform targetTransform, Vector3 position, Bounds extents)
        {
            if (!pullBack) return position;

            position = new Vector3(position.x, elevation, position.z);
            position += targetTransform.TransformDirection(Vector3.back * extents.size.magnitude * pullBackFactor);

            return position;
        }

        private void CalculateCenterAndExtents(
            FeatureCollection featureCollection, 
            out Vector3 position, 
            out Bounds extents
        ) {
            double[] boundingBox = featureCollection.BoundingBoxes ?? featureCollection.DerivedBoundingBoxes();
            int epsgId = featureCollection.EPSGId();

            var realWorldTopLeft = new Coordinate(epsgId, boundingBox[0], boundingBox[1], elevation);
            var realWorldBottomRight = new Coordinate(epsgId, boundingBox[2], boundingBox[3], elevation);
            var topLeft = CoordinateConverter.ConvertTo(realWorldTopLeft, CoordinateSystem.Unity);
            var bottomRight = CoordinateConverter.ConvertTo(realWorldBottomRight, CoordinateSystem.Unity);
            var width = bottomRight.Points[0] - topLeft.Points[0];
            var depth = bottomRight.Points[2] - topLeft.Points[2];
            var center = new Vector3(
               Convert.ToSingle(topLeft.Points[0] + width * .5d),
               0,
               Convert.ToSingle(topLeft.Points[2] + depth * .5d)
            );

            position = new Vector3(center.x, targetGameObject.transform.position.y, center.z);
            extents = new Bounds(
                position,
                new Vector3(Convert.ToSingle(width), Convert.ToSingle(depth), 0f)
            );
        }

        private void Move(Transform targetTransform, Vector3 position)
        {
            if (movingDuration == 0)
            {
                targetTransform.position = position;
                onPositionChanged.Invoke(targetTransform.position);
                return;
            }

            var tween = new MoveToTween(
                this,
                targetTransform.gameObject,
                position,
                targetTransform.rotation,
                movingDuration
            );
            tween.OnCompleted.AddListener(() => onPositionChanged.Invoke(targetTransform.position));
            tween.Play();
        }
    }
}