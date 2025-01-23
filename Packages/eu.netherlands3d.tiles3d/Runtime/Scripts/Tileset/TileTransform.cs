using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Netherlands3D.Coordinates;
namespace Netherlands3D.Tiles3D
{
    [System.Serializable]
    public struct TileTransform
    {

        // memory layout:
        //
        //                row no (=vertical)
        //               |  0   1   2   3
        //            ---+----------------
        //            0  | m00 m10 m20 m30
        // column no  1  | m01 m11 m21 m31
        // (=horiz)   2  | m02 m12 m22 m32
        //            3  | m03 m13 m23 m33

        public double m00;
        public double m01;
        public double m02;
        public double m03;

        public double m10;
        public double m11;
        public double m12;
        public double m13;

        public double m20;
        public double m21;
        public double m22;
        public double m23;

        public double m30;
        public double m31;
        public double m32;
        public double m33;

        public static TileTransform Identity()
        {
            TileTransform result = new TileTransform();
            result.m00 = 1;
            result.m01 = 0;
            result.m02 = 0;
            result.m03 = 0;
            result.m10 = 0;
            result.m11 = 1;
            result.m12 = 0;
            result.m13 = 0;
            result.m20 = 0;
            result.m21 = 0;
            result.m22 = 1;
            result.m23 = 0;
            result.m30 = 0;
            result.m31 = 0;
            result.m32 = 0;
            result.m33 = 1;
            return result;
        }

        public TileTransform(double[] transformValues)
        {
            if (transformValues.Length != 16)
            {
                m00 = 1;
                m01 = 0;
                m02 = 0;
                m03 = 0;
                m10 = 0;
                m11 = 1;
                m12 = 0;
                m13 = 0;
                m20 = 0;
                m21 = 0;
                m22 = 1;
                m23 = 0;
                m30 = 0;
                m31 = 0;
                m32 = 0;
                m33 = 1;
                return;
            }

            m00 = transformValues[0];
            m10 = transformValues[1];
            m20 = transformValues[2];
            m30 = transformValues[3];

            m01 = transformValues[4];
            m11 = transformValues[5];
            m21 = transformValues[6];
            m31 = transformValues[7];

            m02 = transformValues[8];
            m12 = transformValues[9];
            m22 = transformValues[10];
            m32 = transformValues[11];

            m03 = transformValues[12];
            m13 = transformValues[13];
            m23 = transformValues[14];
            m33 = transformValues[15];
        }

        public static TileTransform operator *(TileTransform lhs, TileTransform rhs)
        {
            TileTransform res;
            res.m00 = lhs.m00 * rhs.m00 + lhs.m01 * rhs.m10 + lhs.m02 * rhs.m20 + lhs.m03 * rhs.m30;
            res.m01 = lhs.m00 * rhs.m01 + lhs.m01 * rhs.m11 + lhs.m02 * rhs.m21 + lhs.m03 * rhs.m31;
            res.m02 = lhs.m00 * rhs.m02 + lhs.m01 * rhs.m12 + lhs.m02 * rhs.m22 + lhs.m03 * rhs.m32;
            res.m03 = lhs.m00 * rhs.m03 + lhs.m01 * rhs.m13 + lhs.m02 * rhs.m23 + lhs.m03 * rhs.m33;

            res.m10 = lhs.m10 * rhs.m00 + lhs.m11 * rhs.m10 + lhs.m12 * rhs.m20 + lhs.m13 * rhs.m30;
            res.m11 = lhs.m10 * rhs.m01 + lhs.m11 * rhs.m11 + lhs.m12 * rhs.m21 + lhs.m13 * rhs.m31;
            res.m12 = lhs.m10 * rhs.m02 + lhs.m11 * rhs.m12 + lhs.m12 * rhs.m22 + lhs.m13 * rhs.m32;
            res.m13 = lhs.m10 * rhs.m03 + lhs.m11 * rhs.m13 + lhs.m12 * rhs.m23 + lhs.m13 * rhs.m33;

            res.m20 = lhs.m20 * rhs.m00 + lhs.m21 * rhs.m10 + lhs.m22 * rhs.m20 + lhs.m23 * rhs.m30;
            res.m21 = lhs.m20 * rhs.m01 + lhs.m21 * rhs.m11 + lhs.m22 * rhs.m21 + lhs.m23 * rhs.m31;
            res.m22 = lhs.m20 * rhs.m02 + lhs.m21 * rhs.m12 + lhs.m22 * rhs.m22 + lhs.m23 * rhs.m32;
            res.m23 = lhs.m20 * rhs.m03 + lhs.m21 * rhs.m13 + lhs.m22 * rhs.m23 + lhs.m23 * rhs.m33;

            res.m30 = lhs.m30 * rhs.m00 + lhs.m31 * rhs.m10 + lhs.m32 * rhs.m20 + lhs.m33 * rhs.m30;
            res.m31 = lhs.m30 * rhs.m01 + lhs.m31 * rhs.m11 + lhs.m32 * rhs.m21 + lhs.m33 * rhs.m31;
            res.m32 = lhs.m30 * rhs.m02 + lhs.m31 * rhs.m12 + lhs.m32 * rhs.m22 + lhs.m33 * rhs.m32;
            res.m33 = lhs.m30 * rhs.m03 + lhs.m31 * rhs.m13 + lhs.m32 * rhs.m23 + lhs.m33 * rhs.m33;

            return res;
        }

        
        // Transforms a position by this matrix, without a perspective divide. (fast)
        public Coordinate MultiplyPoint3x4(Coordinate point)
        {
            Coordinate res = new Coordinate(point.CoordinateSystem,0,0,0);
            res.value1 = this.m00 * point.Points[0] + this.m01 * point.Points[1] + this.m02 * point.height + this.m03;
            res.value2= this.m10 * point.Points[0] + this.m11 * point.Points[1] + this.m12 * point.height + this.m13;
            res.value3 = this.m20 * point.Points[0] + this.m21 * point.Points[1] + this.m22 * point.height + this.m23;
            return res;
        }
        public Coordinate MultiplyVector(Coordinate point)
        {
            Coordinate res = new Coordinate(point.CoordinateSystem, 0, 0, 0);
            res.value1 = this.m00 * point.Points[0] + this.m01 * point.Points[1] + this.m02*point.Points[2] ;
            res.value2 = this.m10 * point.Points[0] + this.m11 * point.Points[1] + this.m12 * point.Points[2];
            res.value3 = this.m20 * point.Points[0] + this.m21 * point.Points[1] + this.m22 * point.Points[2];
            return res;
        }
    }
}