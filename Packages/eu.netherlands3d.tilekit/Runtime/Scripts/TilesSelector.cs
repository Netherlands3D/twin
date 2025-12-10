using System;
using Netherlands3D.Coordinates;
using Netherlands3D.Tilekit.ExtensionMethods;
using Netherlands3D.Tilekit.Geometry;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using Debug = System.Diagnostics.Debug;

namespace Netherlands3D.Tilekit
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
        
        // BEAM Search on the nodes (https://en.wikipedia.org/wiki/Beam_search)
        // TODO: This is not beam - this is DFS. Good enough for initial POCcing
        private NativeHashSet<int> Traverse(Tile tile, NativeHashSet<int> traversedTiles)
        {
            try
            {
                // If the tile is not in view - no use to consider it. Just prune the branch from here
                if (!IsInView(tile)) return traversedTiles;
            }
            catch (FailedInViewTest e)
            {
                // if we failed to determine whether the tile is in view, we can't do anything about it now, let's continue and log the error.
                UnityEngine.Debug.LogException(e);
                return traversedTiles;
            }

            // If the LOD is sufficient, mark the tile for rendering and skip its children.
            if (IsLodSufficient(tile))
            {
                traversedTiles.Add(tile.Index);

                return traversedTiles;
            }

            // Explicit tilesets: loop through all children
            var children = tile.Children();
            for (var index = 0; index < children.Length; index++)
            {
                var child = tile.GetChild(index);
                traversedTiles = Traverse(child, traversedTiles);
            }

            return traversedTiles;
        }

        /// <summary>
        /// Perform frustrum culling to determine if the tile -and thus its children- is visible
        /// </summary>
        private bool IsInView(Tile tile)
        {
            try
            {
                var bounds = tile.BoundingVolume.ToBounds().ToLocalCoordinateSystem(CoordinateSystem.RD);
                return GeometryUtility.TestPlanesAABB(frustumPlanes, bounds);
            } 
            catch (Exception e) 
            {
                UnityEngine.Debug.LogException(e);
                throw new FailedInViewTest("Unable to determine whether tile with id " + tile.Index + " is in view, an error occurred");
            }
        }

        private bool IsLodSufficient(Tile tile)
        {
            // Check the LOD criterion (e.g., geometricError vs. distance to the camera).
            return CalculateTileScreenSpaceError(tile) < maximumScreenSpaceError;
        }
        
        private float CalculateTileScreenSpaceError(Tile child)
        {
            BoundsDouble boundsDouble = child.BoundingVolume.ToBounds().ToLocalCoordinateSystem(CoordinateSystem.RD);
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

    public class FailedInViewTest : Exception
    {
        public FailedInViewTest()
        {
        }

        public FailedInViewTest(string message) : base(message)
        {
        }

        public FailedInViewTest(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}