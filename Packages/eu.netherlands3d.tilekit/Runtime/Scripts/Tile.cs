using System;
using UnityEngine;

namespace Netherlands3D.Tilekit
{
    public readonly struct Tile : IEquatable<Tile>
    {
        public int RdX { get; }
        public int RdY { get; }
        public int Size { get; }

        public Tile(int rdX, int rdY, int size)
        {
            RdX = rdX;
            RdY = rdY;
            Size = size;
        }
        
        public bool Equals(Tile other)
        {
            return RdX == other.RdX && RdY == other.RdY && Size == other.Size;
        }
        
        public override bool Equals(object obj)
        {
            return obj is Tile other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(RdX, RdY, Size);
        }
        
        public static bool operator ==(Tile left, Tile right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Tile left, Tile right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return $"Tile({RdX},{RdY},{Size})";
        }
    }
}
