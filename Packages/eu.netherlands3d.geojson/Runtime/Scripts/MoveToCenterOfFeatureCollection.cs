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
        
        [Tooltip(
            "Instead of maintaining the Y of the target game object; move it backwards in local space to fit the " 
            + "whole bounds of the feature collection. This could be used in combination with a Camera " 
            + "-as the target transform- to fit the whole area in view"
        )]
        [SerializeField] private bool fitToBounds;

        [Tooltip("When fitting to bounds: use this factor to make the end result closer or further away, where 1.0 is the default distance.")]
        [SerializeField] private float fittingDistanceModifier = 1.0f;

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
            var bounds = CalculateCenterAndExtents(featureCollection);

            Debug.Log($"Moving {targetGameObject} to {bounds.center}");
            var targetTransform = targetGameObject.transform;

            MoveTo(targetTransform, PullbackToFitBounds(targetTransform, bounds));
        }

        private Vector3 PullbackToFitBounds(Transform targetTransform, Bounds bounds)
        {
            var position = bounds.center;
            if (!fitToBounds) return position;

            position = new Vector3(position.x, elevation, position.z);
            position += targetTransform.TransformDirection(Vector3.back * bounds.size.magnitude * fittingDistanceModifier);

            return position;
        }

        private Bounds CalculateCenterAndExtents(FeatureCollection featureCollection)
        {
            double[] boundingBox = featureCollection.BoundingBoxes ?? featureCollection.DerivedBoundingBoxes();
            int epsgId = featureCollection.EPSGId();
                      
            var realWorldTopRight = new Coordinate(epsgId, boundingBox[2], boundingBox[1], elevation);
            var realWorldBottomLeft = new Coordinate(epsgId, boundingBox[0], boundingBox[3], elevation);
            var center = (realWorldTopRight + realWorldBottomLeft) * 0.5d;
            var size = realWorldTopRight - realWorldBottomLeft;
            Vector3 unityCenter = center.ToUnity();
            Vector3 unitySize = size.ToUnity();
            return new Bounds(
                new Vector3(unityCenter.x, targetGameObject.transform.position.y, unityCenter.z),
                new Vector3(unitySize.x, unitySize.z, 0f)
            );
        }

        private void MoveTo(Transform targetTransform, Vector3 position)
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