using System;
using Netherlands3D.Tilekit.WriteModel;

namespace Netherlands3D.Tilekit.TileSetBuilders
{
    public abstract class QuadTreeBuilder : ITileSetBuilder<ExplicitQuadTreePopulatorSettings>
    {
        public void Build(TileSet tiles, ExplicitQuadTreePopulatorSettings settings)
        {
            // Reset tilestorage to be empty without releasing memory
            tiles.Clear();
            
            // Create the whole tree in the TilesStorage
            int stride = SubtreeSize(settings.Depth - 1);
            AddChildTile(tiles, -1, settings.Depth, stride, tiles.AreaOfInterest);
        }

        static int Pow4(int n)
        {
            int r = 1;
            while (n-- > 0) r *= 4; // avoid float pow
            return r;
        }

        static int SubtreeSize(int depth)
        {
            return depth < 0 ? 0 : (Pow4(depth + 1) - 1) / 3;
        }
        
        private int AddLevelOfTiles(TileSet tiles, BoxBoundingVolume boundingVolume, int tileIndex, int remainingDepth)
        {
            // Work is done, return
            // TODO: Add support for availability: https://docs.ogc.org/cs/22-025r4/22-025r4.html#implicittiling-availability
            // TODO: And allow the warm phase to detect and alter availability, for example WMS only knows whether a tile is available upon loading it
            //    and detecting if it is empty
            if (remainingDepth == 0) return tileIndex;

            int depth = remainingDepth - 1;
            int stride = SubtreeSize(depth - 1);
            
            var (boundingVolumeTopLeft, boundingVolumeTopRight, boundingVolumeBottomRight, boundingVolumeBottomLeft) = boundingVolume.Subdivide2D();

            tileIndex = AddChildTile(tiles, tileIndex, depth, stride, boundingVolumeTopLeft);
            tileIndex = AddChildTile(tiles, tileIndex, depth, stride, boundingVolumeTopRight);
            tileIndex = AddChildTile(tiles, tileIndex, depth, stride, boundingVolumeBottomRight);
            tileIndex = AddChildTile(tiles, tileIndex, depth, stride, boundingVolumeBottomLeft);
            
            return tileIndex;
        }

        private int AddChildTile(TileSet tileSet, int tileIndex, int depth, int stride, BoxBoundingVolume boundingVolume)
        {
            int myIndex = tileIndex + 1;
            ReadOnlySpan<int> children = depth > 0 
                ? stackalloc int[4] { myIndex + 1, myIndex + 1 + stride, myIndex + 1 + stride * 2, myIndex + 1 + stride * 3 } 
                : ReadOnlySpan<int>.Empty;
            
            // Re-use the same content bounding volume as the tile's bounding volume
            var boundingVolumeRef = new BoundingVolumeRef(BoundingVolumeType.Box, myIndex);
            
            ReadOnlySpan<TileContentData> content = stackalloc TileContentData[1]
            {
                new TileContentData(GenerateUrl(tileSet, myIndex, boundingVolume), boundingVolumeRef)
            };
            
            tileIndex = tileSet.AddTile(boundingVolume, 1000 * depth, content, children);

            return AddLevelOfTiles(tileSet, boundingVolume, tileIndex, depth);
        }
        
        protected abstract int GenerateUrl(TileSet tileSet, int tileIndex, BoxBoundingVolume boundingVolume);
    }
    
    public struct ExplicitQuadTreePopulatorSettings
    {
        public int Depth;
    }
}