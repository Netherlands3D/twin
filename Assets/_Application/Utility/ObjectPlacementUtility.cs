using UnityEngine;

namespace Netherlands3D.Twin.Utility
{
    public static class ObjectPlacementUtility
    {
        //TODO make a replacement for this with the new heightmap feature to have a new default 0 plane

        public static Vector3 GetSpawnPoint()
        {
            var camera = Camera.main;
            var cameraTransform = camera.transform;

            var ray = camera.ScreenPointToRay(new Vector2(Screen.width * .5f, Screen.height * .5f));
            var plane = new Plane(Vector3.up, 0);
            if (plane.Raycast(ray, out var distance) == false)
            {
                distance = 10f;
            }

            return cameraTransform.position + cameraTransform.forward * distance;
        }
    }
}
