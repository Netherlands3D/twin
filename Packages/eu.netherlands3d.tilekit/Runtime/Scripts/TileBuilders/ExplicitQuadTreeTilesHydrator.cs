using System;
using Netherlands3D.Tilekit.BoundingVolumes;
using Netherlands3D.Tilekit.WriteModel;

namespace Netherlands3D.Tilekit.TileBuilders
{
    public class ExplicitQuadTreeTilesHydrator : IColdStorageHydrator<ExplicitQuadTreeTilesHydratorSettings>
    {
        public void Build(ColdStorage tiles, ExplicitQuadTreeTilesHydratorSettings settings)
        {
            // Reset tilestorage to be empty without releasing memory
            tiles.Clear();
            
            // Create the whole tree in the TilesStorage
            int stride = SubtreeSize(settings.Depth - 1);
            // int myIndex = 0;

            // var tileIndex = tiles.AddTile(
            //     boundingVolume,
            //     1000 * depth,
            //     new ReadOnlySpan<TileContentData>(new[] { new TileContentData(myIndex, new BoundingVolumeRef(BoundingVolumeType.Box, myIndex)) }),
            //     stackalloc int[] {  myIndex + 1, myIndex + 1 + stride, myIndex + 1 + stride*2, myIndex + 1 + stride*3 } 
            // );
            AddChildTile(tiles, -1, settings.Depth, stride, tiles.AreaOfInterest);
            // AddLevelOfTiles(tiles, boundingVolume, tileIndex, depth);
        }

        static int Pow4(int n)
        {
            int r = 1;
            while (n-- > 0) r *= 4; // avoid float pow
            return r;
        }
        static int SubtreeSize(int depth) // depth >= 0
        {
            // T(depth) = (4^(depth+1) - 1) / 3
            return depth < 0 ? 0 : (Pow4(depth + 1) - 1) / 3;
        }
        private static int AddLevelOfTiles(ColdStorage tiles, BoxBoundingVolume boundingVolume, int tileIndex, int remainingDepth)
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

        private static int AddChildTile(ColdStorage tiles, int tileIndex, int depth, int stride, BoxBoundingVolume boundingVolume)
        {
            int myIndex = tileIndex + 1;
            ReadOnlySpan<int> children = depth > 0 
                ? stackalloc int[4] { myIndex + 1, myIndex + 1 + stride, myIndex + 1 + stride*2, myIndex + 1 + stride*3 } 
                : ReadOnlySpan<int>.Empty;
            ReadOnlySpan<TileContentData> content = stackalloc TileContentData[1]
            {
                new TileContentData(myIndex, new BoundingVolumeRef(BoundingVolumeType.Box, myIndex))
            };
            
            tileIndex = tiles.AddTile(boundingVolume, 1000 * depth, content, children);

            return AddLevelOfTiles(tiles, boundingVolume, tileIndex, depth);
        }
    }
    
    public struct ExplicitQuadTreeTilesHydratorSettings
    {
        public int Depth;
    }
}