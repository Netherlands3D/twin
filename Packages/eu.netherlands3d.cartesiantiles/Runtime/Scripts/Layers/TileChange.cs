using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Netherlands3D.CartesianTiles
{
    [Serializable]
    public struct TileChange : IEquatable<TileChange>
    {

        public TileAction action;
        public int priorityScore;
        public int layerIndex;
        public int X;
        public int Y;

        public bool Equals(TileChange other)
        {
            return (X == other.X && Y == other.Y && layerIndex == other.layerIndex);
        }

    }
}
