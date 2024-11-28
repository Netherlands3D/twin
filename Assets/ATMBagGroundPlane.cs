using Netherlands3D.Coordinates;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Netherlands3D.Twin
{
    public class ATMBagGroundPlane : MonoBehaviour
    {
        public Coordinate coord;

        public void SetCoordinate(Coordinate coord)
        {
            this.coord = coord;
        }
    }
}
