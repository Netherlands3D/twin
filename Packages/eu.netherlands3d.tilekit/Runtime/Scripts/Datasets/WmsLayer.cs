using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Netherlands3D.Coordinates;
using Netherlands3D.Functionalities.Wms;
using Netherlands3D.Tilekit.Optimized;
using Netherlands3D.Tilekit.Optimized.TileSets;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Pool;
using Object = UnityEngine.Object;

namespace Netherlands3D.Tilekit.Datasets
{
    // https://docs.ogc.org/cs/22-025r4/22-025r4.html#toc31 for implicit tiling inspiration
    [RequireComponent(typeof(Timer))]
    public class WmsLayer : MonoBehaviour
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct WarmTile
        {
            public int TileIndex;
            public ulong TextureKey; // 0 = none
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct HotTile
        {
            public int WarmTileIndex;
            // public TextureDecalProjector Projector;
        }

        public TextureDecalProjector textureDecalProjectorPrefab;

        public string CapabilitiesUrl;
        public string Url;
        private Timer timer;
        private RemoteTextureContentImporter importer;
        private Texture2DOverlayRenderer tileRenderer;

        private TilesStorage tiles;
        private readonly Dictionary<int, int> warmIndexByTile = new(); // tileIndex -> index in warmTiles
        private NativeList<WarmTile> warmTiles;
        private NativeList<HotTile> hotTiles;

        // https://service.pdok.nl/prorail/spoorwegen/wms/v1_0?request=GetCapabilities&service=WMS
        // https://service.pdok.nl/prorail/spoorwegen/wms/v1_0?request=GetMap&service=WMS&version=1.3.0&layers=kilometrering&styles=kilometrering&CRS=EPSG%3a28992&bbox=155000%2c464000%2c156000%2c465000&width=1024&height=1024&format=image%2fpng&transparent=true

        // https://service.pdok.nl/kadaster/kadastralekaart/wms/v5_0?request=GetCapabilities&service=WMS
        // https://service.pdok.nl/kadaster/kadastralekaart/wms/v5_0?request=GetMap&service=WMS&version=1.3.0&layers=OpenbareRuimteNaam&styles=standaard%3aopenbareruimtenaam&CRS=EPSG%3a28992&width=1024&height=1024&format=image%2fpng&transparent=true&bbox=154000%2c462000%2c155000%2c463000

        private IEnumerator Start()
        {
            // Wait two frames for the switch to main scene
            yield return null;
            yield return null;

            Url = "https://service.pdok.nl/prorail/spoorwegen/wms/v1_0?request=GetMap&service=WMS&version=1.3.0&layers={layers}&styles={styles}&CRS=EPSG%3a28992&width=1024&height=1024&format=image%2fpng&transparent=true&bbox={bbox}";
            importer = new RemoteTextureContentImporter();
            tileRenderer = new Texture2DOverlayRenderer(new DecalProjectorPool(textureDecalProjectorPrefab, gameObject));
            tiles = new TilesStorage(256);
            // TODO: Should we move the warm and cold tiles management to the TilesStorage if we give it a Generic?
            warmTiles = new NativeList<WarmTile>(256, Allocator.Persistent);
            hotTiles = new NativeList<HotTile>(256, Allocator.Persistent);

            timer = GetComponent<Timer>();
            timer.tick.AddListener(OnTick);
            timer.Resume();

            Enable();
        }

        private void OnTick()
        {
            // TODO: Determine which cold tiles should be warmed
            // OnWarmEnter(new[]{0});
            // TODO: Determine which warm tiles should be heated
            // OnHotEnter(new[]{0});
            // TODO: Determine which hot tiles should be cooled down
            // TODO: Determine which warm tiles should be cooled down
        }

        private void OnDestroy()
        {
            hotTiles.Dispose();
            warmTiles.Dispose();
            tiles.Dispose();
        }

        private void Enable()
        {
            OnColdAlloc();

            var keys = new NativeArray<int>(tiles.GeometricError.Length, Allocator.Temp);
            for (int i = 0; i < tiles.GeometricError.Length; i++) keys[i] = i;
            
            // Fake selection of Warming for now
            OnWarmEnter(keys);
            keys.Dispose();
        }

        private void OnColdAlloc()
        {
            // var capabilities = new WmsGetCapabilities(new Uri(CapabilitiesUrl), response.Data as string);
            //
            // var tileSet = new TileSet();
            // var bbox = capabilities.GetBounds().GlobalBoundingBox;

            int left = 154000;
            int right = 157000;
            int top = 463000;
            int bottom = 466000;
            int depth = 2;

            ExplicitQuadTreeTilesBuilder.Build(
                tiles, 
                BoxBoundingVolume.FromTopLeftAndBottomRight(
                    new double3(left, top, 0), 
                    new double3(right, bottom, 0)
                ), 
                depth
            );
        }

        private void OnColdDealloc()
        {
            tiles.Clear();
        }

        private void OnWarmEnter(ReadOnlySpan<int> candidateTileIndices)
        {
            var urlStringBuilder = new StringBuilder(Url);
            urlStringBuilder.Replace("{layers}", "kilometrering");
            urlStringBuilder.Replace("{styles}", "kilometrering");

            for (var index = 0; index < candidateTileIndices.Length; index++)
            {
                var newUrlStringBuilder = new StringBuilder(urlStringBuilder.ToString());
                var candidate = candidateTileIndices[index];

                // Again - a shortcut but one less than below, I should incorporate the BVref somehow
                var bv = tiles.BoundingVolumes.Boxes[candidate];
                newUrlStringBuilder.Replace("{bbox}", $"{bv.TopLeft.x},{bv.TopLeft.y},{bv.BottomRight.x},{bv.BottomRight.y}");
                var url = newUrlStringBuilder.ToString();
                Debug.Log($"Warming tile {candidate} with url {url}");
                var key = RemoteTextureContentImporter.HashUrl(url);

                var warmIdx = warmTiles.Length;
                warmTiles.AddNoResize(new WarmTile { TileIndex = candidate, TextureKey = key });
                warmIndexByTile[candidate] = warmIdx;

                importer.Import(url);
            }

            OnHotEnter(candidateTileIndices);
        }

        private void OnWarmExit(ReadOnlySpan<int> candidateTileIndices)
        {
            // TODO: Release texture2d
        }

        private void OnHotEnter(ReadOnlySpan<int> candidateTileIndices)
        {
            for (int i = 0; i < candidateTileIndices.Length; i++)
            {
                int tileIndex = candidateTileIndices[i];
                if (!warmIndexByTile.TryGetValue(tileIndex, out var warmIdx)) continue;

                // TODO: shouldn't this be a condition to go to hot? Instead of waiting while hot? or is this better so that we can show a placeholder?
                // TODO: Check if tiles have an empty texture after moving this to the warm phase, and immediately discard the texture and prevent this tile
                //   from using rendered representation
                var wt = warmTiles[warmIdx];
                if (!importer.TryGet(wt.TextureKey, out var tex))
                {
                    // Optional: wait and retry when it completes (still decoupled)
                    importer.GetAsync(wt.TextureKey).Then(_ =>
                    {
                        // Re-try only this tile; keeps it minimal and local
                        OnHotEnter(stackalloc int[] { tileIndex });
                    });
                    continue;
                }

                tileRenderer.Create(tiles.Get(tileIndex), tex);
            }
        }

        private void OnHotExit(ReadOnlySpan<int> candidateTileIndices)
        {
            for (int i = 0; i < candidateTileIndices.Length; i++)
            {
                tileRenderer.Release(tiles.Get(candidateTileIndices[i]));
            }
        }
    }

    // TODO: consider making this non-static and an adapter
    public class ExplicitQuadTreeTilesBuilder
    {
        public static void Build(TilesStorage tiles, BoxBoundingVolume boundingVolume, int depth)
        {
            // Reset tilestorage to be empty without releasing memory
            tiles.Clear();
            
            // Create the whole tree in the TilesStorage
            int stride = SubtreeSize(depth);

            var tileIndex = tiles.AddTile(
                boundingVolume,
                1,
                new ReadOnlySpan<TileContentData>(new[] { new TileContentData(0, new BoundingVolumeRef(BoundingVolumeType.Box, 0)) }),
                stackalloc int[] { stride, stride*2, stride*3, stride*4 } 
            );
            AddLevelOfTiles(tiles, boundingVolume, tileIndex, depth);
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
        private static int AddLevelOfTiles(TilesStorage tiles, BoxBoundingVolume boundingVolume, int tileIndex, int remainingDepth)
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

        private static int AddChildTile(TilesStorage tiles, int tileIndex, int depth, int stride, BoxBoundingVolume boundingVolume)
        {
            int myIndex = tileIndex + 1;
            ReadOnlySpan<int> children = depth > 0 
                ? stackalloc int[4] { myIndex + 1, myIndex + 1 + stride, myIndex + 1 + stride*2, myIndex + 1 + stride*3 } 
                : ReadOnlySpan<int>.Empty;
            ReadOnlySpan<TileContentData> content = stackalloc TileContentData[1]
            {
                new TileContentData(0, new BoundingVolumeRef(BoundingVolumeType.Box, myIndex))
            };
            
            tileIndex = tiles.AddTile(boundingVolume, 10, content, children);

            return AddLevelOfTiles(tiles, boundingVolume, tileIndex, depth);
        }
       
    }

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

        public void Release(Tile tile)
        {
            projectorPool.Release(Get(tile));
        }
    }

    public class DecalProjectorPool
    {
        private readonly TextureDecalProjector textureDecalProjectorPrefab;
        private readonly GameObject parent;
        private readonly ObjectPool<TextureDecalProjector> projectorPool;

        public DecalProjectorPool(TextureDecalProjector textureDecalProjectorPrefab, GameObject parent)
        {
            this.textureDecalProjectorPrefab = textureDecalProjectorPrefab;
            this.parent = parent;

            projectorPool = new ObjectPool<TextureDecalProjector>(
                CreateProjectorForPool,
                actionOnGet: GetProjectorFromPool,
                actionOnRelease: ReleaseProjectorToPool
            );
        }

        private void GetProjectorFromPool(TextureDecalProjector projector)
        {
            projector.gameObject.SetActive(true);
        }

        private void ReleaseProjectorToPool(TextureDecalProjector projector)
        {
            projector.gameObject.SetActive(false);
            projector.SetTexture(null);
        }

        private TextureDecalProjector CreateProjectorForPool()
        {
            return Object.Instantiate(textureDecalProjectorPrefab, parent.transform);
        }

        public TextureDecalProjector Get()
        {
            return projectorPool.Get();
        }

        public void Release(TextureDecalProjector projector)
        {
            projectorPool.Release(projector);
        }
    }
}