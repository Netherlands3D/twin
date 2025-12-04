using System.Collections.Generic;
using Netherlands3D.Coordinates;
using Netherlands3D.Functionalities.Wms;
using Netherlands3D.Tilekit.Geometry;
using Unity.Mathematics;
using UnityEngine;

namespace Netherlands3D.Tilekit.Renderers
{
    public class Texture2DOverlayRenderer
    {
        private readonly Dictionary<int, TextureDecalProjector> projectors = new();
        private readonly DecalProjectorPool projectorPool;

        public Texture2DOverlayRenderer(DecalProjectorPool projectorPool)
        {
            this.projectorPool = projectorPool;
        }

        public void Create(Tile tile, Texture2D texture)
        {
            var bv = tile.BoundingVolume.AsBox();
            var bounds = BoundsDouble.FromMinAndMax(new double3(bv.TopLeft.x, bv.TopLeft.y, 0), new double3(bv.BottomRight.x, bv.BottomRight.y, 0));

            // TODO: No in-position coordinate conversions?
            // TODO: Should all coordinates be in Unity (local) space and have a base transform that we can change based on shifting?
            var worldPositionTopLeft = new Coordinate(CoordinateSystem.RD, bounds.Min.x, bounds.Min.y, bounds.Min.z).ToUnity();
            var worldPositionBottomRight = new Coordinate(CoordinateSystem.RD, bounds.Max.x, bounds.Max.y, bounds.Max.z).ToUnity();

            var projector = projectorPool.Get();
            var width = worldPositionBottomRight.x - worldPositionTopLeft.x;
            var depth = worldPositionBottomRight.z - worldPositionTopLeft.z;
            var center = new Vector2(worldPositionTopLeft.x + width / 2f, worldPositionTopLeft.z + depth / 2f);

            projector.transform.position = new Vector3(center.x, projector.transform.position.y, center.y);
            projector.SetSize(width, depth, 1000);
            projector.SetTexture(texture);
            projectors[tile.Index] = projector;
        }

        public TextureDecalProjector Get(Tile tile)
        {
            return projectors[tile.Index];
        }

        public TextureDecalProjector Get(int tileIndex)
        {
            return projectors[tileIndex];
        }

        public void Release(Tile tile)
        {
            projectorPool.Release(Get(tile));
        }

        public void Release(int tileIndex)
        {
            projectorPool.Release(Get(tileIndex));
        }
    }
}