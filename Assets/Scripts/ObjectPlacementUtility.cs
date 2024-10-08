using UnityEngine;

namespace Netherlands3D.Twin
{
    public static class ObjectPlacementUtility
    {
        public static Vector3 GetSpawnPoint()
        {
            var camera = Camera.main;
            var cameraTransform = camera.transform;

            var ray = camera.ScreenPointToRay(new Vector2(Screen.width / 2f, Screen.height / 2f));
            var plane = new Plane(Vector3.up, 0);
            var intersect = plane.Raycast(ray, out float distance);
            if (!intersect)
            {
                distance = 10f;
            }

            return cameraTransform.position + cameraTransform.forward * distance;
        }
    }
}
