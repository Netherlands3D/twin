using System;
using System.Runtime.Serialization;
using Netherlands3D.Tilekit.TileSets.BoundingVolumes;
using Netherlands3D.Tilekit.TileSets.ImplicitTiling;
using Unity.Collections;
using UnityEngine;

namespace Netherlands3D.Tilekit.TileSets
{
    public class Tile
    {
        /// <summary>
        /// A generated ID for internal use - we use this in hashtables to build quick access indices. 
        /// </summary>
        public string Id { get; } = Guid.NewGuid().ToString();
    
        public IBoundingVolume BoundingVolume;
        public double GeometricError;
        public TileContents TileContents = new();
        public Matrix4x4 Transform;
        public Tiles Children = new();
        public IImplicitTilingScheme ImplicitTiling = new None();
        public MethodOfRefinement Refine = MethodOfRefinement.Replace;

        public Tile(IBoundingVolume boundingVolume, double geometricError)
        {
            BoundingVolume = boundingVolume;
            GeometricError = geometricError;
        }
    }
}
