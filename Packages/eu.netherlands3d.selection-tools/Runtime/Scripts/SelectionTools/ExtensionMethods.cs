using System;
using UnityEngine;

namespace Netherlands3D.SelectionTools
{
    [Obsolete("These extension methods are extracted from Netherlands3D to remove a dependency. A more suitable solution should be found to not have double code")]
    public static class ExtensionMethods
    {
        /// <summary>
        /// Get the position of a screen point in world coordinates ( on a plane )
        /// </summary>
        /// <param name="screenPoint">The point in screenpoint coordinates</param>
        /// <returns></returns>
        public static Vector3 GetCoordinateInWorld(this Camera camera, Vector3 screenPoint, Plane worldPlane, float maxSelectionDistanceFromCamera = Mathf.Infinity)
        {
            var screenRay = camera.ScreenPointToRay(screenPoint);

            worldPlane.Raycast(screenRay, out float distance);
            var samplePoint = screenRay.GetPoint(Mathf.Min(maxSelectionDistanceFromCamera, distance));

            return samplePoint;
        }

        public static bool IsInLayerMask(this GameObject obj, LayerMask mask)
        {
            return ((mask.value & (1 << obj.layer)) > 0);
        }
    }
}
