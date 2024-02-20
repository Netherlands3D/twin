using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Netherlands3D.Twin
{
    public static class ObjectPlacementUtility
    {
        public static Vector3 GetSpawnPoint()
        {            
            var ray = Camera.main.ScreenPointToRay(new Vector2(Screen.width / 2, Screen.height / 2));
            var plane = new Plane(Vector3.up, 0);
            var intersect = plane.Raycast(ray, out float distance);
            if (!intersect)
                distance = 10f;

            var spawnPoint = Camera.main.transform.position + Camera.main.transform.forward * distance;
            return spawnPoint;
        }

        public static Vector3 GetOpticalRaycasterSpawnPoint()
        {
            var raycaster = Object.FindAnyObjectByType<OpticalRaycaster>();
            if(raycaster != null)
            {
                return raycaster.GetWorldPointAtCameraScreenPoint(Camera.main, new Vector3(Screen.width / 2, Screen.height / 2, 0));
            }
            else
            {
                Debug.LogWarning("No OpticalRaycaster found in the scene, falling back to default spawn point");
                return GetSpawnPoint();
            }
        }
    }
}
