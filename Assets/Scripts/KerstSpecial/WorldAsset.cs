using Netherlands3D.Coordinates;
using Netherlands3D.Twin.FloatingOrigin;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Netherlands3D.Twin
{
    public class WorldAsset : MonoBehaviour
    {
        public double lat;
        public double lon;
        public float YRotation;

        public GameObject prefab;
        private bool grounded = false;

        // Start is called before the first frame update
        void Start()
        {
            Coordinate nextCoord = new Coordinate(CoordinateSystem.WGS84, lat, lon, 0);
            Vector3 unityCoord = nextCoord.ToUnity();
            transform.position = unityCoord;

            prefab = Instantiate(prefab);
            WorldTransform wt = prefab.AddComponent<WorldTransform>();
            GameObjectWorldTransformShifter shifter = prefab.AddComponent<GameObjectWorldTransformShifter>();
            wt.SetShifter(shifter);
            prefab.transform.position = transform.position;
            prefab.transform.rotation = Quaternion.AngleAxis(YRotation, Vector3.up);
        }

        // Update is called once per frame
        void Update()
        {
            if (!grounded)
            {
                RaycastHit hit;
                if (Physics.Raycast(Vector3.up * 100, Vector3.down * 1000, out hit))
                {
                    Vector3 pos = prefab.transform.position;
                    prefab.transform.position = new Vector3(pos.x, hit.point.y, pos.z);
                    grounded = true;
                }
            }
        }
    }
}
