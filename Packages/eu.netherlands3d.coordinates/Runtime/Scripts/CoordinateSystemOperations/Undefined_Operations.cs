using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Netherlands3D.Coordinates
{
    /// <summary>
    /// Operations for RD
    /// </summary>
    class Undefined_Operations : CoordinateSystemOperation
    {
        public override string Code()
        {
            return "undefined";
        }

        public override int NorthingIndex()
        {
            return 1;
        }
        public override int EastingIndex()
        {
            return 0;
        }
        public override int AxisCount()
        {
            return 0;
        }
        public override Coordinate ConvertFromWGS84LatLonH(Coordinate coordinate)
        {
            return new Coordinate(CoordinateSystem.Undefined);
        }

        public override Coordinate ConvertToWGS84LatLonH(Coordinate coordinate)
        {
            return new Coordinate(CoordinateSystem.Undefined);
        }

        public override bool CoordinateIsValid(Coordinate coordinate)
        {
            return false;
        }

        public override CoordinateSystemGroup GetCoordinateSystemGroup()
        {
            return CoordinateSystemGroup.None;
        }

        public override CoordinateSystemType GetCoordinateSystemType()
        {
            return CoordinateSystemType.None;
        }

        public override Vector3WGS GlobalUpDirection(Coordinate coordinate)
        {
            return new Vector3WGS();
        }

        public override Vector3WGS LocalUpDirection(Coordinate coordinate)
        {
            return new Vector3WGS();
        }

        public override Vector3WGS Orientation()
        {
            return new Vector3WGS();
        }
    }
}
