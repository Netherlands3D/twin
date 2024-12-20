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
        public float XRotation;
        public float size = 1;
        public float height;
        public bool overrideHeight = false;
        public bool addMeshCollider = false;

        public GameObject prefab;
        private bool grounded = false;

        protected Vector3 startPosition;
        protected Coordinate startCoord;

        private RaceController raceController;

        // Start is called before the first frame update
        public virtual void Start()
        {
            raceController = FindObjectOfType<RaceController>();
            startCoord = new Coordinate(CoordinateSystem.WGS84, lat, lon, 0);
            Vector3 unityCoord = startCoord.ToUnity();
            transform.position = unityCoord;

            prefab = Instantiate(prefab);
            WorldTransform wt = prefab.AddComponent<WorldTransform>();
            GameObjectWorldTransformShifter shifter = prefab.AddComponent<GameObjectWorldTransformShifter>();
            wt.SetShifter(shifter);
            wt.onPostShift.AddListener((a, b) => {
                Vector3 pos = startCoord.ToUnity();
                startPosition = new Vector3(pos.x, height, pos.z);
                OnSetStartPosition(startPosition);
            });
            prefab.transform.position = transform.position;
            prefab.transform.rotation = Quaternion.Euler(XRotation, YRotation, 0);
            prefab.transform.localScale = Vector3.one * size;
            prefab.transform.SetParent(transform, false);

            if(addMeshCollider)
                prefab.AddComponent<MeshCollider>();
        }

        // Update is called once per frame
        public virtual void Update()
        {
            float dist = Vector3.Distance(raceController.playerCollider.gameObject.transform.position, prefab.transform.position);
            if (prefab.gameObject.activeSelf && dist > 250)
                prefab.gameObject.SetActive(false);
            else if(!prefab.gameObject.activeSelf && dist  < 250)
                prefab.gameObject.SetActive(true);


            if (!grounded && RaceController.isReadyToMove)
            {
                if (overrideHeight)
                {
                    Vector3 pos = startCoord.ToUnity();
                    startPosition = new Vector3(pos.x, height, pos.z);
                    OnSetStartPosition(startPosition);
                    grounded = true;
                    return;
                }

                RaycastHit[] hits = new RaycastHit[8];
                Physics.RaycastNonAlloc(startCoord.ToUnity() + Vector3.up * 100, Vector3.down, hits, 1000);
                for(int i = 0; i < hits.Length; i++) 
                {
                    if (hits[i].collider is not MeshCollider)
                        continue;
                    
                    Vector3 pos = startCoord.ToUnity();
                    startPosition = new Vector3(pos.x, hits[i].point.y, pos.z);                       
                    OnSetStartPosition(startPosition);
                    grounded = true;
                }
            }
        }

        protected virtual void OnSetStartPosition(Vector3 startPosition)
        {
            if (overrideHeight)
                startPosition.y = height;

            prefab.transform.position = startPosition;
        }
    }
}
