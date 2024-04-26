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
        public override Coordinate ConvertFromWGS84LatLonH(Coordinate coordinate)
        {
            CoordinateSystemOperation converter = CoordinateSystems.operators[CoordinateSystem.RDNAP];
            
            Coordinate result = converter.ConvertFromWGS84LatLonH(coordinate);

            Coordinate output = new Coordinate(CoordinateSystem.RD, result.Points[0], result.Points[1]);
            output.extraLongitudeRotation = result.extraLongitudeRotation;
            output.extraLattitudeRotation = result.extraLattitudeRotation;
            return output;
        }

        public override Coordinate ConvertToWGS84LatLonH(Coordinate coordinate)
        {
            CoordinateSystemOperation converter = CoordinateSystems.operators[CoordinateSystem.RDNAP];
            //add third dimension to point  
            double[] tempPoints = new double[3] { coordinate.Points[0], coordinate.Points[1], 0 };
            coordinate.Points = tempPoints;

            Coordinate result = converter.ConvertToWGS84LatLonH(coordinate);
            result.Points[2] = 0;
            return result;
        }

        public override bool CoordinateIsValid(Coordinate coordinate)
        {
            if (coordinate.Points.Length != 2)
            {
                return false;
            }
            CoordinateSystemOperation converter = CoordinateSystems.operators[CoordinateSystem.RDNAP];
            double[] newPoints = new double[3] {coordinate.Points[0], coordinate.Points[1],0 };
            Coordinate testCoordinate = new Coordinate(CoordinateSystem.RDNAP, newPoints);
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
            double[] tempPoints = new double[3] { coordinate.Points[0], coordinate.Points[1], 0 };
            coordinate.Points = tempPoints;
            return converter.GlobalUpDirection(coordinate);
        }

        public override Vector3WGS LocalUpDirection(Coordinate coordinate)
        {
            CoordinateSystemOperation converter = CoordinateSystems.operators[CoordinateSystem.RDNAP];
            double[] tempPoints = new double[3] { coordinate.Points[0], coordinate.Points[1], 0 };
            coordinate.Points = tempPoints;
            return converter.LocalUpDirection(coordinate);
        }

        public override Vector3WGS Orientation()
        {
            CoordinateSystemOperation converter = CoordinateSystems.operators[CoordinateSystem.RDNAP];
            return converter.Orientation();
        }
    }
}
