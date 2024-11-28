using Netherlands3D.Coordinates;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Netherlands3D.Twin
{
    public class AddGroundPlanesForCoordinates : MonoBehaviour
    {
        public float scale = 10f;
        public List<Vector2> groundPlanesLonLat = new List<Vector2>();
        [SerializeField] private GameObject groundPlanePrefab;

        private void Start()
        {
            foreach (var pos in groundPlanesLonLat) 
            {
                Coordinate coord = new Coordinate(CoordinateSystem.WGS84_LatLon, pos.x, pos.y);
                Vector3 unityPosition = coord.ToUnity();
                unityPosition.y = 0;
                GameObject plane = GameObject.Instantiate(groundPlanePrefab, unityPosition, Quaternion.identity);                
                plane.transform.SetParent(transform);
                plane.transform.localScale = Vector3.one * scale;
                ATMBagGroundPlane groundPlane = plane.GetComponent<ATMBagGroundPlane>();
                groundPlane.SetCoordinate(coord);
                plane.SetActive(false);
            }
        }
    }
}
