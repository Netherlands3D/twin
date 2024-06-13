using UnityEngine;

namespace Netherlands3D.Twin
{
    public static class ObjectPlacementUtility
    {
        public static Vector3 GetSpawnPoint()
        {
            var camera = Camera.main;
            var ray = camera.ScreenPointToRay(new Vector2(Screen.width / 2, Screen.height / 2));
            var plane = new Plane(Vector3.up, 0);
            var intersect = plane.Raycast(ray, out float distance);
            if (!intersect)
                distance = 10f;

            var spawnPoint = camera.transform.position + camera.transform.forward * distance;
            return spawnPoint;
        }
    }
}
