using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Netherlands3D.Coordinates
{
    abstract class CoordinateSystemOperation
    {
        public abstract bool CoordinateIsValid(Coordinate coordinate);
        public abstract CoordinateSystemGroup GetCoordinateSystemGroup();
        public abstract CoordinateSystemType GetCoordinateSystemType();
        public abstract Coordinate ConvertFromWGS84LatLonH(Coordinate coordinate);
        public abstract Coordinate ConvertToWGS84LatLonH(Coordinate coordinate);
        public abstract Vector3WGS Orientation();
        public abstract Vector3WGS GlobalUpDirection(Coordinate coordinate);
        public abstract Vector3WGS LocalUpDirection(Coordinate coordinate);
        public abstract int EastingIndex();
        public abstract int NorthingIndex();
        public abstract int AxisCount();
        public abstract string Code();


    }
}
