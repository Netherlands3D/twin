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

            Coordinate output = new Coordinate(CoordinateSystem.RD, result.x, result.y);
            output.extraLongitudeRotation = result.extraLongitudeRotation;
            output.extraLattitudeRotation = result.extraLattitudeRotation;
            return output;
        }

        public override Coordinate ConvertToWGS84LatLonH(Coordinate coordinate)
        {
            CoordinateSystemOperation converter = CoordinateSystems.operators[CoordinateSystem.RDNAP];
            //add third dimension to point  
            //double[] tempPoints = new double[3] { coordinate.Points[0], coordinate.Points[1], 0 };
            //coordinate.Points = tempPoints;
            //Coordinate result = converter.ConvertToWGS84LatLonH(coordinate);
            //result.Points[2] = 0;
            Coordinate newCoordinate = new Coordinate(coordinate.CoordinateSystem, coordinate.x, coordinate.y, 0);
            Coordinate result = converter.ConvertToWGS84LatLonH(newCoordinate);
            result = new Coordinate(result.CoordinateSystem, result.x, result.y, 0);
            return result;
        }

        public override bool CoordinateIsValid(Coordinate coordinate)
        {
            if (coordinate.PointsLength != 2)
            {
                return false;
            }
            CoordinateSystemOperation converter = CoordinateSystems.operators[CoordinateSystem.RDNAP];
            //double[] newPoints = new double[3] {coordinate.x, coordinate.y,0 };
            Coordinate testCoordinate = new Coordinate(CoordinateSystem.RDNAP, coordinate.x, coordinate.y, 0);
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
            //double[] tempPoints = new double[3] { coordinate.Points[0], coordinate.Points[1], 0 };
            //coordinate.Points = tempPoints;
            Coordinate result = new Coordinate(coordinate.CoordinateSystem, coordinate.x, coordinate.y, 0);
            return converter.GlobalUpDirection(result);
        }

        public override Vector3WGS LocalUpDirection(Coordinate coordinate)
        {
            CoordinateSystemOperation converter = CoordinateSystems.operators[CoordinateSystem.RDNAP];
            //double[] tempPoints = new double[3] { coordinate.Points[0], coordinate.Points[1], 0 };
            //coordinate.Points = tempPoints;
            Coordinate result = new Coordinate(coordinate.CoordinateSystem, coordinate.x, coordinate.y, 0);
            return converter.LocalUpDirection(coordinate);
        }

        public override Vector3WGS Orientation()
        {
            CoordinateSystemOperation converter = CoordinateSystems.operators[CoordinateSystem.RDNAP];
            return converter.Orientation();
        }
    }
}
