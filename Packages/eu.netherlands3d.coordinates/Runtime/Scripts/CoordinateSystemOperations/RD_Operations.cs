using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Netherlands3D.Coordinates
{
    /// <summary>
    /// Operations for RD
    /// </summary>
    class RD_Operations : CoordinateSystemOperation
    {
        public override string Code()
        {
            return "28992";
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
            return 2;
        }
        public override Coordinate ConvertFromWGS84LatLonH(Coordinate coordinate)
        {
            CoordinateSystemOperation converter = CoordinateSystems.operators[CoordinateSystem.RDNAP];
            Coordinate result = converter.ConvertFromWGS84LatLonH(coordinate);
            Coordinate output = new Coordinate(CoordinateSystem.RD, result.value1, result.value2);
            output.extraLongitudeRotation = result.extraLongitudeRotation;
            output.extraLattitudeRotation = result.extraLattitudeRotation;
            return output;
        }

        public override Coordinate ConvertToWGS84LatLonH(Coordinate coordinate)
        {
            CoordinateSystemOperation converter = CoordinateSystems.operators[CoordinateSystem.RDNAP];
            //add third dimension to point              
            Coordinate newCoordinate = new Coordinate(coordinate.CoordinateSystem, coordinate.value1, coordinate.value2, 0);
            Coordinate result = converter.ConvertToWGS84LatLonH(newCoordinate);
            result = new Coordinate(result.CoordinateSystem, result.value1, result.value2, 0);
            return result;
        }

        public override bool CoordinateIsValid(Coordinate coordinate)
        {
            if (coordinate.PointsLength != 2)
            {
                return false;
            }
            CoordinateSystemOperation converter = CoordinateSystems.operators[CoordinateSystem.RDNAP];
            Coordinate testCoordinate = new Coordinate(CoordinateSystem.RDNAP, coordinate.value1, coordinate.value2, 0);
            return converter.CoordinateIsValid(testCoordinate);

        }

        public override CoordinateSystemGroup GetCoordinateSystemGroup()
        {
            return CoordinateSystemGroup.RD;
        }

        public override CoordinateSystemType GetCoordinateSystemType()
        {
            return CoordinateSystemType.Projected;
        }

        public override Vector3WGS GlobalUpDirection(Coordinate coordinate)
        {
            CoordinateSystemOperation converter = CoordinateSystems.operators[CoordinateSystem.RDNAP];
            Coordinate result = new Coordinate(coordinate.CoordinateSystem, coordinate.value1, coordinate.value2, 0);
            return converter.GlobalUpDirection(result);
        }

        public override Vector3WGS LocalUpDirection(Coordinate coordinate)
        {
            CoordinateSystemOperation converter = CoordinateSystems.operators[CoordinateSystem.RDNAP];
            Coordinate result = new Coordinate(coordinate.CoordinateSystem, coordinate.value1, coordinate.value2, 0);
            return converter.LocalUpDirection(coordinate);
        }

        public override Vector3WGS Orientation()
        {
            CoordinateSystemOperation converter = CoordinateSystems.operators[CoordinateSystem.RDNAP];
            return converter.Orientation();
        }
    }
}
