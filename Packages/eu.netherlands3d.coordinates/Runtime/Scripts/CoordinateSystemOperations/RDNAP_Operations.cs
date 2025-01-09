using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Netherlands3D.Coordinates
{/// <summary>
/// Operations for RDNAP
/// </summary>
    class RDNAP_Operations : CoordinateSystemOperation
    {
        public override string Code()
        {
            return "7415";
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
            return 3;
        }
        public override CoordinateSystemGroup GetCoordinateSystemGroup()
        {
            return CoordinateSystemGroup.RD;
        }

        public static byte[] RDCorrectionX = null;
        public static byte[] RDCorrectionY = null;
        public static byte[] RDCorrectionZ = null;

        private static double[] Rp = new double[] { 0, 1, 2, 0, 1, 3, 1, 0, 2 };
        private static double[] Rq = new double[] { 1, 1, 1, 3, 0, 1, 3, 2, 3 };
        private static double[] Rpq = new double[] { 190094.945, -11832.228, -114.221, -32.391, -0.705, -2.340, -0.608, -0.008, 0.148 };
        //setup coefficients for Y-calculation
        private static double[] Sp = new double[] { 1, 0, 2, 1, 3, 0, 2, 1, 0, 1 };
        private static double[] Sq = new double[] { 0, 2, 0, 2, 0, 1, 2, 1, 4, 4 };
        private static double[] Spq = new double[] { 309056.544, 3638.893, 73.077, -157.984, 59.788, 0.433, -6.439, -0.032, 0.092, -0.054 };

        //setup coefficients for lattitude-calculation
        private static double[] Kp = { 0, 2, 0, 2, 0, 2, 1, 4, 2, 4, 1 };
        private static double[] Kq = { 1, 0, 2, 1, 3, 2, 0, 0, 3, 1, 1 };
        private static double[] Kpq = { 3235.65389, -32.58297, -0.24750, -0.84978, -0.06550, -0.01709, -0.00738, 0.00530, -0.00039, 0.00033, -0.00012 };
        //setup coefficients for longitude-calculation
        private static double[] Lp = { 1, 1, 1, 3, 1, 3, 0, 3, 1, 0, 2, 5 };
        private static double[] Lq = { 0, 1, 2, 0, 3, 1, 1, 2, 4, 2, 0, 0 };
        private static double[] Lpq = { 5260.52916, 105.94684, 2.45656, -0.81885, 0.05594, -.05607, 0.01199, -0.00256, 0.00128, 0.00022, -0.00022, 0.00026 };

        private static double refRDX = 155000;
        private static double refRDY = 463000;
        private static double refLon = 5.38720621;
        private static double refLat = 52.15517440;

        static Double RDCorrection(double x, double y, string direction, byte[] bytes)
        {
            if (direction != "Z")
            {
                if (XYisValid(x, y) == false)
                {
                    return 0;
                }
            }

            double value = 0;

            if (direction == "X")
            {
                value = -0.185;
            }
            else if (direction == "Y")
            {
                value = -0.232;
            }

            double Xmin;
            double Xmax;
            double Ymin;
            double Ymax;
            int sizeX;
            int sizeY;

            int dataNumber;
            sizeX = BitConverter.ToInt16(bytes, 4);
            sizeY = BitConverter.ToInt16(bytes, 6);
            Xmin = BitConverter.ToDouble(bytes, 8);
            Xmax = BitConverter.ToDouble(bytes, 16);
            Ymin = BitConverter.ToDouble(bytes, 24);
            Ymax = BitConverter.ToDouble(bytes, 32);

            double columnWidth = (Xmax - Xmin) / sizeX;
            double locationX = Math.Floor((x - Xmin) / columnWidth);
            double rowHeight = (Ymax - Ymin) / sizeY;
            double locationY = (long)Math.Floor((y - Ymin) / rowHeight);

            if (x < Xmin || x > Xmax)
            {
                return value;
            }
            if (y < Ymin || y > Ymax)
            {
                return value;
            }

            dataNumber = (int)(locationY * sizeX + locationX);

            // do linear interpolation on the grid
            if (locationX < sizeX && locationY < sizeY)
            {
                float bottomLeft = BitConverter.ToSingle(bytes, 56 + (dataNumber * 4));
                float bottomRight = BitConverter.ToSingle(bytes, 56 + ((dataNumber + 1) * 4));
                float topLeft = BitConverter.ToSingle(bytes, 56 + ((dataNumber + sizeX) * 4));
                float topRight = BitConverter.ToSingle(bytes, 56 + ((dataNumber + sizeX + 1) * 4));

                double YDistance = ((y - Ymin) % rowHeight) / rowHeight;
                double YOrdinaryLeft = ((topLeft - bottomLeft) * YDistance) + bottomLeft;
                double YOrdinaryRigth = ((topRight - bottomRight) * YDistance) + bottomRight;

                double XDistance = ((x - Xmin) % columnWidth) / columnWidth;
                value += ((YOrdinaryRigth - YOrdinaryLeft) * XDistance) + YOrdinaryLeft;
            }
            else
            {
                float myFloat = BitConverter.ToSingle(bytes, 56 + (dataNumber * 4));
                value += myFloat;
            }

            return value;
        }

        public override Coordinate ConvertFromWGS84LatLonH(Coordinate coordinate)
        {
            //coordinates of basepoint in RD


            //coordinates of basepoint in WGS84



            double DeltaLon = 0.36 * (coordinate.y - refLon);
            double DeltaLat = 0.36 * (coordinate.x - refLat);

            //calculate X
            double DeltaX = 0;
            for (int i = 0; i < Rpq.Length; i++)
            {
                DeltaX += Rpq[i] * Math.Pow(DeltaLat, Rp[i]) * Math.Pow(DeltaLon, Rq[i]);
            }
            double X = DeltaX + refRDX;

            //calculate Y
            double DeltaY = 0;
            for (int i = 0; i < Spq.Length; i++)
            {
                DeltaY += Spq[i] * Math.Pow(DeltaLat, Sp[i]) * Math.Pow(DeltaLon, Sq[i]);
            }
            double Y = DeltaY + refRDY;

            if (RDCorrectionX == null)
            {
                RDCorrectionX = Resources.Load<TextAsset>("x2c").bytes;
                RDCorrectionY = Resources.Load<TextAsset>("y2c").bytes;
                RDCorrectionZ = Resources.Load<TextAsset>("nlgeo04").bytes;
            }
            double correctionX = RDCorrection(X, Y, "X", RDCorrectionX);
            double correctionY = RDCorrection(X, Y, "Y", RDCorrectionY);
            X -= correctionX;
            Y -= correctionY;


            double h = coordinate.z - RDCorrection(coordinate.y, coordinate.x, "Z", RDCorrectionZ);
            Coordinate result = new Coordinate(CoordinateSystem.RDNAP, (float)X, (float)Y, (float)h);
            result.extraLattitudeRotation = coordinate.x + coordinate.extraLattitudeRotation - refLat;
            result.extraLongitudeRotation = coordinate.y + coordinate.extraLongitudeRotation - refLon;
            return result;



        }

        public override Coordinate ConvertToWGS84LatLonH(Coordinate coordinate)
        {
            double x = coordinate.x;
            double y = coordinate.y;
            double nap = coordinate.z;

            if (RDCorrectionX == null)
            {
                RDCorrectionX = Resources.Load<TextAsset>("x2c").bytes;
                RDCorrectionY = Resources.Load<TextAsset>("y2c").bytes;
                RDCorrectionZ = Resources.Load<TextAsset>("nlgeo04").bytes;
            }

            double correctionX = RDCorrection(x, y, "X", RDCorrectionX);
            double correctionY = RDCorrection(x, y, "Y", RDCorrectionY);

            double DeltaX = (x + correctionX - refRDX) * Math.Pow(10, -5);
            double DeltaY = (y + correctionY - refRDY) * Math.Pow(10, -5);

            //calculate latitude
            double Deltalat = 0;
            for (int i = 0; i < Kpq.Length; i++)
            {
                Deltalat += Kpq[i] * Math.Pow(DeltaX, Kp[i]) * Math.Pow(DeltaY, Kq[i]);
            }
            Deltalat = Deltalat / 3600;
            double lat = Deltalat + refLat;

            //calculate longitude
            double Deltalon = 0;
            for (int i = 0; i < Lpq.Length; i++)
            {
                Deltalon += Lpq[i] * Math.Pow(DeltaX, Lp[i]) * Math.Pow(DeltaY, Lq[i]);
            }
            Deltalon = Deltalon / 3600;
            double lon = Deltalon + refLon;


            double h = nap + RDCorrection(lon, lat, "Z", RDCorrectionZ);
            //output height missing
            Coordinate result = new Coordinate(CoordinateSystem.WGS84_LatLonHeight, lat, lon, h);

            result.extraLattitudeRotation = refLat - result.x;
            result.extraLongitudeRotation = refLon - result.y;
            return result;
        }

        public override bool CoordinateIsValid(Coordinate coordinate)
        {
            if (coordinate.PointsLength != 3)
            {
                return false;
            }

            return XYisValid(coordinate.x, coordinate.y);
        }

        static bool XYisValid(double x, double y)
        {
            bool inRange = false;
            if (x > -7000 && x < 300000)
            {
                if (y > 289000 && y < 629000)
                {
                    inRange = true;
                }
            }
            return inRange;
        }

        public override CoordinateSystemType GetCoordinateSystemType()
        {
            return CoordinateSystemType.Projected;
        }

        public override Vector3WGS GlobalUpDirection(Coordinate coordinate)
        {
            Coordinate latlon = ConvertToWGS84LatLonH(coordinate);
            return new Vector3WGS(refLon, refLat, 0);
        }

        public override Vector3WGS LocalUpDirection(Coordinate coordinate)
        {
            Coordinate latlon = ConvertToWGS84LatLonH(coordinate);
            return new Vector3WGS(refLon, refLat, 0);
        }

        public override Vector3WGS Orientation()
        {
            return new Vector3WGS(refLon, refLat, 0);
        }
    }
}
