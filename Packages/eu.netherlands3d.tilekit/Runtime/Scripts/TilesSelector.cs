using System;
using Netherlands3D.Tilekit.ExtensionMethods;
using Netherlands3D.Tilekit.Optimized.TileSets;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using Debug = System.Diagnostics.Debug;

namespace Netherlands3D.Tilekit.Optimized
{
    public class TilesSelector
    {
        private readonly float maximumScreenSpaceError;
        
        // Cached property to reduce number of allocations
        private Plane[] frustumPlanes = new Plane[6];
        private readonly Transform mainCameraTransform;

        public TilesSelector(float maximumScreenSpaceError = 5f, Camera camera = null)
        {
            this.maximumScreenSpaceError = maximumScreenSpaceError;
            camera ??= Camera.main;
            Debug.Assert(camera != null, nameof(camera) + " != null");
            mainCameraTransform = camera.transform;
        }

        public NativeHashSet<int> Select(NativeHashSet<int> tilesSelection, Tile tile, Plane[] frustum, float featherAmount = 1f)
        {
            frustumPlanes = FeatherFrustumPlanes(frustum, featherAmount);
            
            return Traverse(tile, tilesSelection);
        }

        /// <summary>
        /// Increase or decrease the viewport that in which tiles are selected by a factor. 1 means no increase or
        /// decrease happens, higher than 1 will load more tiles and lower than 1 will start to omit tiles in view. 
        /// </summary>
        /// <remarks>
        /// A value other than 1 will introduce extra allocations because we do not want to change the original array as
        /// this might give unexpected side-effects.
        /// </remarks>
        protected virtual Plane[] FeatherFrustumPlanes(Plane[] frustumPlanes, float featherPercentage)
        {
            // If no feathering is required, return the original array and prevent allocations
            if (Math.Abs(featherPercentage - 1f) < .0001f) return frustumPlanes;

            // Create a copy of the frustum planes
            Plane[] featheredPlanes = (Plane[])frustumPlanes.Clone();

            // Modify the copy
            for (int i = 0; i < featheredPlanes.Length; i++)
            {
                featheredPlanes[i].distance += featheredPlanes[i].distance * featherPercentage;
            }

            return featheredPlanes;
        }
        
        // Depth First Search on the nodes
        private NativeHashSet<int> Traverse(Tile tile, NativeHashSet<int> traversedTiles)
        {
            if (!IsInView(tile)) return traversedTiles;
            
            // If the LOD is sufficient, mark the tile for rendering and skip its children.
            if (IsLodSufficient(tile))
            {
                traversedTiles.Add(tile.Index);

                return traversedTiles;
            }

            // if (tile.ImplicitTiling.SubdivisionScheme is not SubdivisionScheme.None)
            // {
                // TraverseImplicit(tile, tile, 1, traversedTiles);
                
                // return traversedTiles;
            // }
            
            // Explicit tilesets: loop through all children
            var children = tile.Children();
            for (var index = 0; index < children.Count; index++)
            {
                traversedTiles = Traverse(tile.GetChild(children[index]), traversedTiles);
            }

            return traversedTiles;
        }

        private NativeHashSet<int> TraverseImplicit(Tile rootTile, Tile tile, int level, NativeHashSet<int> traversedTiles)
        {
            // TODO: Add support for an implicit system
            // Divide BoundingVolume according to type (octree or quadtree)
            // Half geometric error of parent tile into half for children
            
            // implicit traversal is always done from a root tile - consider passing the root tile
            // along the chain when doing implicit to copy it - and thus introduce a copy constructor?
            
            // WARNING:
            // In order to maintain numerical stability during this subdivision process, the actual bounding volumes
            // should not be computed progressively by subdividing a non-root tile volume. Instead, the exact bounding
            // volumes should be computed directly for a given level.
            
            // Let the extent of the root bounding volume along one dimension d be (mind, maxd). The number of bounding
            // volumes along that dimension for a given level is 2level. The size of each bounding volume at this level,
            // along dimension d, is sized = (maxd - mind) / 2level. The extent of the bounding volume of a child can
            // then be computed directly as (mind + sized * i, mind + sized * (i + 1)), where i is the index of the
            // child in dimension d.

            // Tile coords: https://github.com/CesiumGS/3d-tiles/blob/main/specification/ImplicitTiling/README.adoc#tile-coordinates

            return traversedTiles;
        }

        /// <summary>
        /// Perform frustrum culling to determine if the tile -and thus its children- is visible
        /// </summary>
        private bool IsInView(Tile tile)
        {
            // TODO: Can we cache the projection? This is now done for each tile and that will make it heavier
            return GeometryUtility.TestPlanesAABB(
                frustumPlanes, 
                tile.BoundingVolume.ToBounds()
            );
        }

        private bool IsLodSufficient(Tile tile)
        {
            // Check the LOD criterion (e.g., geometricError vs. distance to the camera).
            return CalculateTileScreenSpaceError(tile) < maximumScreenSpaceError;
        }
        
        private float CalculateTileScreenSpaceError(Tile child)
        {
            BoundsDouble boundsDouble = child.BoundingVolume.ToBounds();
            var cameraPosition = mainCameraTransform.position;
            
            var distanceToCamera = Vector3.Distance(
                cameraPosition, 
                boundsDouble.ClosestPoint(new double3(cameraPosition)).ToVector3()
            );

            var sse = distanceToCamera < 0.1f
                ? float.MaxValue
                : (float)child.GeometricError / distanceToCamera;

            return sse;
        }
    }
}