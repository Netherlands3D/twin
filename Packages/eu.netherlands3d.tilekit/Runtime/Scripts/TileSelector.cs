using System;
using Netherlands3D.Tilekit.TileSets;
using UnityEngine;

namespace Netherlands3D.Tilekit
{
    [CreateAssetMenu(menuName = "Tilekit/TileSelector", fileName = "TileSelector", order = 0)]
    public class TileSelector : ScriptableObject
    {
        [SerializeField] private Projector projector;
        
        [Tooltip("Influences how quickly we switch to more detailed tiles, default: 5; higher is faster and thus higher quality")]
        [Range(1, 100)]
        [SerializeField] private float maximumScreenSpaceError = 5f;
        
        [Tooltip("By what factor to increase or decrease the camera frustum for the purpose of preloading tiles. A number above 1 will load more tiles just outside the frustrum, below 1 it decrease the area it will render tiles in")]
        [Range(0f, 2f)]
        [SerializeField] private float featherAmount = 1f;

        // Cached property to reduce number of allocations
        private Plane[] frustumPlanes = new Plane[6];
        private Transform mainCameraTransform;

        public Tiles Select(TileSet tileSet, Plane[] frustum)
        {
            frustumPlanes = FeatherFrustumPlanes(frustum, this.featherAmount);
            
            return Traverse(tileSet.Root);
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
        
        // Depth First Search on the nodes -> this is explicit tiles currently; we need another version for implicit
        private Tiles Traverse(Tile tile, Tiles traversedTiles = null)
        {
            // Can we cache the previous traversal and verify for each traversed cell if it is still valid - and then
            // append newly traversed cells?
            traversedTiles ??= new();

            if (!IsInView(tile)) return traversedTiles;
            
            // If the LOD is sufficient, mark the tile for rendering and skip its children.
            if (IsLodSufficient(tile))
            {
                traversedTiles.Add(tile);

                return traversedTiles;
            }
            
            foreach (var child in tile.Children)
            {
                traversedTiles = Traverse(child, traversedTiles);
            }

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
                projector.ToUnityUnits(tile.BoundingVolume.ToBounds())
            );
        }

        private bool IsLodSufficient(Tile tile)
        {
            // Check the LOD criterion (e.g., geometricError vs. distance to the camera).
            return CalculateTileScreenSpaceError(tile) < maximumScreenSpaceError;
        }
        
        private float CalculateTileScreenSpaceError(Tile child)
        {
            Bounds boundsDouble = child.BoundingVolume.ToBounds();
            var cameraPosition = MainCameraTransform().position;
            
            var distanceToCamera = Vector3.Distance(
                cameraPosition, 
                projector.ToUnityUnits(boundsDouble.ClosestPoint(cameraPosition))
            );

            var sse = distanceToCamera < 0.1f
                ? float.MaxValue
                : (float)child.GeometricError / distanceToCamera;

            return sse;
        }

        private Transform MainCameraTransform()
        {
            // Cache extern call to transform
            if (!mainCameraTransform)
            {
                mainCameraTransform = Camera.main.transform;
            }

            return mainCameraTransform;
        }
    }
}