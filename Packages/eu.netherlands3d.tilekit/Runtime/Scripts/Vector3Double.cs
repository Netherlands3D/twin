using System;
using UnityEngine;

namespace Netherlands3D.Tilekit
{
    [Serializable]
    public struct Vector3Double
    {
        public double x;
        public double y;
        public double z;

        public Vector3Double(double x, double y, double z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public static Vector3Double operator +(Vector3Double a, Vector3Double b)
        {
            return new Vector3Double(a.x + b.x, a.y + b.y, a.z + b.z);
        }

        public static Vector3Double operator -(Vector3Double a, Vector3Double b)
        {
            return new Vector3Double(a.x - b.x, a.y - b.y, a.z - b.z);
        }

        public static Vector3Double operator *(Vector3Double a, double d)
        {
            return new Vector3Double(a.x * d, a.y * d, a.z * d);
        }

        public static Vector3Double operator /(Vector3Double a, double d)
        {
            return new Vector3Double(a.x / d, a.y / d, a.z / d);
        }

        public static Vector3Double Min(Vector3Double a, Vector3Double b)
        {
            return new Vector3Double(Math.Min(a.x, b.x), Math.Min(a.y, b.y), Math.Min(a.z, b.z));
        }

        public static Vector3Double Max(Vector3Double a, Vector3Double b)
        {
            return new Vector3Double(Math.Max(a.x, b.x), Math.Max(a.y, b.y), Math.Max(a.z, b.z));
        }

        public static implicit operator Vector3(Vector3Double v)
        {
            return new Vector3((float)v.x, (float)v.y, (float)v.z);
        }

        public static implicit operator Vector3Double(Vector3 v)
        {
            return new Vector3Double(v.x, v.y, v.z);
        }

        public override string ToString()
        {
            return $"({x}, {y}, {z})";
        }
    }
}