using System;
using UnityEngine;

namespace Netherlands3D.Tilekit
{
    public struct Dimensions
    {
        public double Width { get; }
        public double Height { get; }
        public double Depth { get; }

        public Dimensions(double width, double height, double depth)
        {
            Width = width;
            Height = height;
            Depth = depth;
        }

        public override bool Equals(object obj)
        {
            return obj is Dimensions other &&
                   Math.Abs(Width - other.Width) < 0.000001f &&
                   Math.Abs(Height - other.Height) < 0.000001f &&
                   Math.Abs(Depth - other.Depth) < 0.000001f;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Width, Height, Depth);
        }

        public override string ToString()
        {
            return $"Dimensions(Width: {Width}, Height: {Height}, Depth: {Depth})";
        }

        public static implicit operator Vector3(Dimensions size)
        {
            return new Vector3((float)size.Width, (float)size.Depth, (float)size.Height);
        }

        public static implicit operator Dimensions(Vector3 vector)
        {
            return new Dimensions(vector.x, vector.z, vector.y);
        }

        public static implicit operator Dimensions(Vector3Int vector)
        {
            return new Dimensions(vector.x, vector.z, vector.y);
        }

        public static implicit operator Dimensions(Vector3Double vector)
        {
            return new Dimensions(vector.x, vector.z, vector.y);
        }
    }
}