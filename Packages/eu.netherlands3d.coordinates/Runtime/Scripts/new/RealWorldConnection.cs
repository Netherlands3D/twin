using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Netherlands3D.Coordinates
{
    public class RealWorldConnection : MonoBehaviour
    {
        [Header("origin")]
        public CoordinateSystem coordinateSystem = CoordinateSystem.RDNAP;
        public double X = 120000;
        public double Y=480000;
        public double Z=0;
        public Coordinate origin;

        [Header("used CoordinateSystem")]
        public CoordinateSystem ConnectedcoordinateSystem = CoordinateSystem.WGS84_ECEF;

        // Start is called before the first frame update
        void Start()
        {
            Settings.ConnectedCoordinateSystem = ConnectedcoordinateSystem;
            Settings.SetOrigin(new Coordinate(coordinateSystem, X, Y, Z));
        }

        // Update is called once per frame
        void Update()
        {
        
        }
    }
}
