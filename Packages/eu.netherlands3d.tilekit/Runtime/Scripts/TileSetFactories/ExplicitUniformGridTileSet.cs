using Netherlands3D.Tilekit.TileSets;
using Netherlands3D.Tilekit.TileSets.BoundingVolumes;
using UnityEngine;

namespace Netherlands3D.Tilekit.TileSetFactories
{
    [CreateAssetMenu(menuName = "Netherlands3D/Tilekit/Tilesets/Explicit Uniform Grid")]
    public class ExplicitUniformGridTileSet : BaseTileSetFactory
    {
        [SerializeField] private Vector3 center = Vector3.zero;
        [SerializeField] private int width = 10;
        [SerializeField] private int height = 10;
        [SerializeField] private int tileWidth = 10;
        [SerializeField] private int tileHeight = 10;
        [SerializeField] private int baseGeometricError = 10000;
        
        public override TileSet CreateTileSet()
        {
            var worldSpaceMinX = center.x - (tileWidth * width * .5f);
            var worldSpaceMaxX = center.x + (tileWidth * width * .5f);
            var worldSpaceMinY = center.y - (tileHeight * height * .5f);
            var worldSpaceMaxY = center.y + (tileHeight * height * .5f);
            
            var totalBoundingVolume = new RegionBoundingVolume(
                worldSpaceMinX, 
                worldSpaceMinY, 
                worldSpaceMaxX, 
                worldSpaceMaxY, 
                0, // Not important: 2D tiles, or is it? 
                0 // Not important: 2D tiles, or is it?
            );

            var root = new Tile(new BoundingVolume(totalBoundingVolume), baseGeometricError);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    var childBoundingVolume = new RegionBoundingVolume(
                        worldSpaceMinX + (x * tileWidth),
                        worldSpaceMinY + (y * tileHeight),
                        worldSpaceMaxX + ((x + 1) * tileWidth),
                        worldSpaceMaxY + ((y + 1) * tileHeight),
                        totalBoundingVolume.MinHeight,
                        totalBoundingVolume.MaxHeight
                    );
                    // Geometric error is set to 0, meaning: this is a leaf that is always visible. We should
                    // TODO: review the specification of 3D Tiles whether the TileSelector should always treat the leaf
                    // as present independent of GeoMetric error
                    var child = new Tile(new BoundingVolume(childBoundingVolume), 0);
                    // It is missing Tile Contents, but we make do without for now
                    root.Children.Add(child);
                }
            }
            
            return new TileSet(root);
        }
    }
}