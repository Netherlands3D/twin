using System;
using System.Runtime.Serialization;
using Netherlands3D.Tilekit.TileSets.ImplicitTiling;
using UnityEngine;

namespace Netherlands3D.Tilekit.TileSets
{
    [DataContract(Namespace = "eu.netherlands3d.tilekit.tilesets", Name = "Tile")]
    public class Tile
    {
        /// <summary>
        /// A generated ID for internal use - we use this in hashtables to build quick access indices. 
        /// </summary>
        public Guid Id { get; } = Guid.NewGuid();
    
        public BoundingVolume BoundingVolume;
        public double GeometricError;
        public TileContents TileContents = new();
        public Matrix4x4 Transform;
        public Metadata Metadata = new();
        public Tiles Children = new();
        public ImplicitTilingScheme ImplicitTiling = new None();
        public MethodOfRefinement Refine = MethodOfRefinement.Replace;

        public Tile(BoundingVolume boundingVolume, double geometricError)
        {
            BoundingVolume = boundingVolume;
            GeometricError = geometricError;
        }
    }
}