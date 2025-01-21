using RuntimeHandle;
using UnityEngine;

namespace Netherlands3D.Twin
{
    public class TransformAxes : MonoBehaviour
    {
        [Header("Locks")]
        [Space(5)]
        public bool PositionLocked = false;
        public bool RotationLocked = false;
        public bool ScaleLocked = false;

        [Header("Allowed axes")]
        [Space(5)]
        public HandleAxes positionAxes = HandleAxes.XYZ;
        public HandleAxes rotationAxes = HandleAxes.XYZ;
        public HandleAxes scaleAxes = HandleAxes.XYZ;       
    }
}
