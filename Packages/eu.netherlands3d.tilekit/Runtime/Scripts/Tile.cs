using System;
using UnityEngine;

namespace Netherlands3D.Tilekit
{
    public readonly struct Tile : IEquatable<Tile>
    {
        public Vector2Int Coordinate { get; }
        public int Size { get; }

        public Tile(Vector2Int coordinate, int size)
        {
            Coordinate = coordinate;
            Size = size;
        }
        
        public bool Equals(Tile other)
        {
            return Coordinate.Equals(other.Coordinate) && Size == other.Size;
        }
        
        public override bool Equals(object obj)
        {
            return obj is Tile other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Coordinate, Size);
        }
        
        public static bool operator ==(Tile left, Tile right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Tile left, Tile right)
        {
            return !left.Equals(right);
        }
    }
}
