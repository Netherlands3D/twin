using System.Collections;
using System.Collections.Generic;
using Netherlands3D.Twin.FloatingOrigin;
using UnityEngine;

namespace Netherlands3D.Twin
{
    /// <summary>
    /// Shifts the origin of the world when the Camera object is far away from the origin.
    /// The GameObjectWorldTransformShifter will make sure the Unity transform is corrected accordingly like any other GameObject
    /// </summary>
    [RequireComponent(typeof(GameObjectWorldTransformShifter),typeof(WorldTransform))]
    public class OriginShifter : MonoBehaviour
    {
        [SerializeField] private float shiftDistance = 10000f; //Km from 0,0,0
        [Tooltip("Will be searched if not set.")] public Origin floatingOrigin;

        private void Awake() {
            if(!floatingOrigin)
                floatingOrigin = FindObjectOfType<Origin>();
        }

        void Update()
        {
            var flatPosition  = new Vector3(transform.position.x, 0, transform.position.z);
            if (flatPosition.magnitude > shiftDistance)
            {
                var newOriginCoordinate = new Coordinates.Coordinate(Coordinates.CoordinateSystem.Unity, flatPosition.x,flatPosition.y,flatPosition.z);
                floatingOrigin.Coordinate = newOriginCoordinate;
            }
        }
    }
}
