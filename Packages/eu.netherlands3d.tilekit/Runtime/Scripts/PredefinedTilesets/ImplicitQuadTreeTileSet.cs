using KindMen.Uxios;
using Netherlands3D.Tilekit.TileSets;
using Netherlands3D.Tilekit.TileSets.BoundingVolumes;
using Netherlands3D.Tilekit.TileSets.ImplicitTiling;
using UnityEngine;

namespace Netherlands3D.Tilekit.PredefinedTilesets
{
    [CreateAssetMenu(menuName = "Netherlands3D/Tilekit/WebMapper or XYZ Tiles")]
    public class ImplicitQuadTreeTileSet : TileSetFactory
    {
        [SerializeField] private string url = "https://images.huygens.knaw.nl/webmapper/maps/pw-1985/{level}/{x}/{y}.png";
        
        /// <summary>
        /// EPSG:3857-based
        /// </summary>
        [SerializeField] private double worldSpaceMinX = -20037508.34d;
        [SerializeField] private double worldSpaceMinY = -20037508.34d;
        [SerializeField] private double worldSpaceMaxX = 20037508.34d;
        [SerializeField] private double worldSpaceMaxY = 20037508.34d;
        [SerializeField] private int baseGeometricError = 10000;
        
        public override TileSet CreateTileSet()
        {
            var totalBoundingVolume = new RegionBoundingVolume(
                worldSpaceMinX, 
                worldSpaceMinY, 
                worldSpaceMaxX, 
                worldSpaceMaxY, 
                0, // Not important: 2D tiles, or is it? 
                0 // Not important: 2D tiles, or is it?
            );

            var root = new Tile(totalBoundingVolume, baseGeometricError);
            root.TileContents.Add(new TileContent()
            {
                Uri = new TemplatedUri(url)
            });

            root.ImplicitTiling = new QuadTree()
            {
                SubtreeLevels = 0,
                AvailableLevels = 20
            };

            // subdivisionScheme	Constant for all descendant tiles
            // refine	Constant for all descendant tiles
            // boundingVolume	Divided into four or eight parts depending on the subdivisionScheme
            // Each child’s geometricError is half of its parent’s geometricError


            // NOTE  In order to maintain numerical stability during this subdivision process, the actual bounding
            // volumes should not be computed progressively by subdividing a non-root tile volume. Instead, the exact
            // bounding volumes should be computed directly for a given level.

            // Let the extent of the root bounding volume along one dimension d be (mind, maxd). The number of bounding
            // volumes along that dimension for a given level is 2level. The size of each bounding volume at this level,
            // along dimension d, is sized = (maxd — mind) / 2level. The extent of the bounding volume of a child
            // can then be computed directly as (mind + sized * i, mind + sized * (i + 1)), where i is the index of the
            // child in dimension d.
            
            // The computed tile boundingVolume and geometricError can be overridden with tile metadata, if desired.
            // Content bounding volumes are not computed automatically but they may be provided by content metadata.
            // Tile and content bounding volumes shall maintain spatial coherence.
            
            // Tile coordinates are a tuple of integers that uniquely identify a tile. Tile coordinates are either (level, x, y) for quadtrees or (level, x, y, z) for octrees. All tile coordinates are 0-indexed.
            // level is 0 for the implicit root tile. This tile’s children are at level 1, and so on.
            //    x, y, and z coordinates define the location of the tile within the level.

            return new TileSet(root);
        }
    }
}