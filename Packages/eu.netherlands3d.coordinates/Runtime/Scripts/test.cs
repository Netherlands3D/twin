using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Netherlands3D.Coordinates;


namespace Netherlands3D
{
    public class test : MonoBehaviour
    {
        public Vector3 resultXplus;
        public double distance;
        public Vector3 resultYplus;
        // Start is called before the first frame update
        void Start()
        {
            Coordinate RDNAPCenter = new Coordinate(CoordinateSystem.RDNAP, 155000, 463000, 0);

            CoordinateSystems.connectedCoordinateSystem = CoordinateSystem.ETRS89_ECEF;
            Coordinate origin = new Coordinate(CoordinateSystem.RDNAP, 165000, 463000, 0);
            CoordinateSystems.SetOrigin(origin);

            Coordinate setup = new Coordinate(CoordinateSystem.RDNAP,165000, 463000, 0);
            //setup = setup.Convert(CoordinateSystem.ETRS89_ECEF);
            //setup = new Coordinate(CoordinateSystem.ETRS89_ECEF, setup.Points);

            transform.rotation = setup.RotationToUnityUp();




            resultXplus = setup.ToUnity();
            //Coordinate test = new Coordinate(CoordinateSystem.RDNAP, 121000d, 480000d, 0d);
            //Coordinate result = setup;
            //Coordinate difference = result - CoordinateSystems.CoordinateAtUnityOrigin;

            //distance = Mathf.Sqrt((float)(difference.Points[0] * difference.Points[0] + difference.Points[1] * difference.Points[1] + difference.Points[2] * difference.Points[2]));

            
            ////result = result.Convert(CoordinateSystem.RDNAP);
            ////resultXplus = new Vector3((float)result.Points[0], (float)result.Points[1], (float)result.Points[2]);

            //test = new Coordinate(CoordinateSystem.RDNAP, 120000d, 481000d, 0d);
            //result = test.Convert(CoordinateSystem.WGS84_ECEF);

            //result = result.Convert(CoordinateSystem.RDNAP);
            //resultYplus = new Vector3((float)result.Points[0], (float)result.Points[1], (float)result.Points[2]);
        }

        // Update is called once per frame
        void Update()
        {
        
        }
    }
}
