using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Netherlands3D.Coordinates
{
    public class SetupCRS : MonoBehaviour
    {
        public CoordinateSystem BasisCoordinateSystem;
        [Header("origin")]
        public CoordinateSystem OriginCoordinateSystem;
        public double[] CoordinateValues = new double[3];
        // Start is called before the first frame update
        void Start()
        {
            Coordinate origin = new Coordinate(OriginCoordinateSystem, CoordinateValues);
            //TODO check if coordainte is Valid

            CoordinateSystems.connectedCoordinateSystem = BasisCoordinateSystem;
            CoordinateSystems.SetOrigin(origin);
        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}
