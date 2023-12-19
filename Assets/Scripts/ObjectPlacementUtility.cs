using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Netherlands3D.Twin
{
    public class ObjectPlacementUtility : MonoBehaviour
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
    }
}
