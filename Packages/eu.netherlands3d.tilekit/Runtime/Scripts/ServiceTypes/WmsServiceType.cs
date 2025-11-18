using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Netherlands3D.Coordinates;
using Netherlands3D.Functionalities.Wms;
using Netherlands3D.Tilekit.Archetypes;
using Netherlands3D.Tilekit.BoundingVolumes;
using Netherlands3D.Tilekit.ContentImporters;
using Netherlands3D.Tilekit.ExtensionMethods;
using Netherlands3D.Tilekit.Renderers;
using Netherlands3D.Tilekit.TileBuilders;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Netherlands3D.Tilekit.ServiceTypes
{
    // https://docs.ogc.org/cs/22-025r4/22-025r4.html#toc31 for implicit tiling inspiration
    [RequireComponent(typeof(Timer))]
    public class WmsServiceType : MonoBehaviour
    {
        private const int InitialCapacity = 1024;

        public TextureDecalProjector textureDecalProjectorPrefab;

        public string CapabilitiesUrl;
        public string Url;
        private Timer timer;
        private RemoteTextureContentImporter importer;
        private Texture2DOverlayRenderer tileRenderer;

        private RasterArchetype archetype;
        private readonly Dictionary<int, int> warmIndexByTile = new(); // tileIndex -> index in warmTiles
        private TilesSelector tileSelector;

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
            tileSelector = new TilesSelector();
            archetype = new RasterArchetype(InitialCapacity, Allocator.Persistent);

            timer = GetComponent<Timer>();
            timer.tick.AddListener(OnTick);
            timer.Resume();

            OnColdAlloc();
        }

        private void OnTick()
        {
            var tilesInFrustrum = new NativeHashSet<int>(1024, Allocator.Temp);
            var warmTileIndices = new NativeHashSet<int>(1024, Allocator.Temp);
            var shouldWarmUp = new NativeHashSet<int>(1024, Allocator.Temp);
            var shouldFreeze = new NativeHashSet<int>(1024, Allocator.Temp);
            
            
            tileSelector.Select(tilesInFrustrum, archetype.Cold.Get(0), GeometryUtility.CalculateFrustumPlanes(Camera.main));
            
            // Tiles are selected using a zoned approach, where we can select a subset of tiles that are in a certain zone. 
            // There is a warm and hot zone, and when tiles who are active in either zone migrate to another zone - they
            // should either warm/heat up or cool down/freeze.
            
            // Tiles are not only selected on their spatial property - but also on their LOD or temporal properties, where 
            // selection criteria is based on information from the global system - such as camera position and time of day
            
            foreach (var warmTile in archetype.Warm)
            {
                warmTileIndices.Add(warmTile.TileIndex);
                if (tilesInFrustrum.Contains(warmTile.TileIndex) == false)
                {
                    shouldFreeze.Add(warmTile.TileIndex);
                }
            }
            foreach (var tileInFrustum in tilesInFrustrum)
            {
                if (warmTileIndices.Contains(tileInFrustum) == false)
                {
                    shouldWarmUp.Add(tileInFrustum);
                }
            }
            OnWarmUp(shouldWarmUp.ToNativeArray(Allocator.Temp));
            // TODO: Heat up
            // TODO: Cool down
            OnFreeze(shouldFreeze.ToNativeArray(Allocator.Temp));
            
            tilesInFrustrum.Dispose();
            warmTileIndices.Dispose();
            shouldWarmUp.Dispose();
            shouldFreeze.Dispose();
        }

        private void OnDestroy()
        {
            archetype.Dispose();
        }
        
        private void OnColdAlloc()
        {
            // var capabilities = new WmsGetCapabilities(new Uri(CapabilitiesUrl), response.Data as string);
            //
            // var tileSet = new TileSet();
            // var bbox = capabilities.GetBounds().GlobalBoundingBox;

            int left = 153000;
            int right = 158000;
            int top = 462000;
            int bottom = 467000;
            int depth = 4;

            ExplicitQuadTreeTilesBuilder.Build(
                archetype.Cold, 
                BoxBoundingVolume.FromTopLeftAndBottomRight(
                    new double3(left, top, 0), 
                    new double3(right, bottom, 0)
                ), 
                depth
            );
        }

        private void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying)
                return;

            if (!archetype.Warm.IsCreated || !archetype.Hot.IsCreated)
                return;

            var tile = archetype.Cold.Get(0);
            DrawTileGizmo(tile);
        }

        private void DrawTileGizmo(Tile tile, int height = 0)
        {
            var bounds = tile.BoundingVolume.ToBounds().ToLocalCoordinateSystem(CoordinateSystem.RD);
            
            Gizmos.color = Color.white;
            // TODO: can we make this O(1)?
            for (var index = 0; index < archetype.Warm.Length; index++)
            {
                if (archetype.Warm[index].TileIndex == tile.Index) Gizmos.color = Color.yellow;
            }
            // TODO: can we make this O(1)?
            for (var index = 0; index < archetype.Hot.Length; index++)
            {
                if (archetype.Warm[archetype.Hot[index].WarmTileIndex].TileIndex == tile.Index) Gizmos.color = Color.red;
            }

            if (Gizmos.color != Color.white)
                Gizmos.DrawWireCube(bounds.center, bounds.size);

            for (int i = 0; i < tile.Children().Count; i++)
            {
                DrawTileGizmo(tile.GetChild(i), height + 1);
            }
        }

        public void OnWarmUp(ReadOnlySpan<int> candidateTileIndices)
        {
            var urlStringBuilder = new StringBuilder(Url);
            urlStringBuilder.Replace("{layers}", "kilometrering");
            urlStringBuilder.Replace("{styles}", "kilometrering");

            for (var index = 0; index < candidateTileIndices.Length; index++)
            {
                var newUrlStringBuilder = new StringBuilder(urlStringBuilder.ToString());
                var candidate = candidateTileIndices[index];

                // Again - a shortcut but one less than below, I should incorporate the BVref somehow
                var bv = archetype.Cold.BoundingVolumes.Boxes[candidate];
                newUrlStringBuilder.Replace("{bbox}", $"{bv.TopLeft.x},{bv.TopLeft.y},{bv.BottomRight.x},{bv.BottomRight.y}");
                var url = newUrlStringBuilder.ToString();
                Debug.Log($"Warming tile {candidate} with url {url}");
                var key = RemoteTextureContentImporter.HashUrl(url);

                var warmIdx = archetype.Warm.Length;
                archetype.Warm.AddNoResize(new RasterArchetype.WarmTile { TileIndex = candidate, TextureKey = key });
                warmIndexByTile[candidate] = warmIdx;

                importer.Import(url);
            }

            OnHeatUp(candidateTileIndices);
        }

        public void OnHeatUp(ReadOnlySpan<int> candidateTileIndices)
        {
            for (int i = 0; i < candidateTileIndices.Length; i++)
            {
                int tileIndex = candidateTileIndices[i];
                
                // If it aint' warm - it cannot get hot
                if (!warmIndexByTile.TryGetValue(tileIndex, out var warmIdx)) continue;

                // TODO: shouldn't this be a condition to go to hot? Instead of waiting while hot? or is this better so that we can show a placeholder?
                // TODO: Check if tiles have an empty texture after moving this to the warm phase, and immediately discard the texture and prevent this tile
                //   from using rendered representation
                var wt = archetype.Warm[warmIdx];
                if (!importer.TryGet(wt.TextureKey, out var tex))
                {
                    // Optional: wait and retry when it completes (still decoupled)
                    importer.GetAsync(wt.TextureKey).Then(_ =>
                    {
                        // Re-try only this tile; keeps it minimal and local
                        OnHeatUp(stackalloc int[] { tileIndex });
                    });
                    continue;
                }

                archetype.Hot.AddNoResize(new RasterArchetype.HotTile { WarmTileIndex = warmIdx });
                tileRenderer.Create(archetype.Cold.Get(tileIndex), tex);
            }
        }

        public void OnCooldown(ReadOnlySpan<int> candidateTileIndices)
        {
            for (int i = 0; i < candidateTileIndices.Length; i++)
            {
                var tile = archetype.Cold.Get(candidateTileIndices[i]);

                tileRenderer.Release(tile);

                for (var index = 0; index < archetype.Hot.Length; index++)
                {
                    var hotTile = archetype.Hot[index];
                    if (archetype.Warm[hotTile.WarmTileIndex].TileIndex == tile.Index)
                    {
                        archetype.Hot.RemoveAtSwapBack(index);
                        index--;
                    }
                }

            }
        }

        public void OnFreeze(ReadOnlySpan<int> candidateTileIndices)
        {
            OnCooldown(candidateTileIndices);

            for (int i = 0; i < candidateTileIndices.Length; i++)
            {
                int tileIndex = candidateTileIndices[i];

                if (!warmIndexByTile.TryGetValue(tileIndex, out int warmIdx))
                    continue; // already cold or never warmed

                // Evict before we change the list
                var warm = archetype.Warm[warmIdx];
                importer.TryEvict(warm.TextureKey);

                int lastIdx = archetype.Warm.Length - 1;

                if (warmIdx != lastIdx)
                {
                    // Tile that will be moved down
                    RasterArchetype.WarmTile moved = archetype.Warm[lastIdx];

                    // Move it into the gap
                    archetype.warm[warmIdx] = moved;

                    // Fix dictionary for moved tile
                    warmIndexByTile[moved.TileIndex] = warmIdx;

                    // Fix any hot tiles that pointed at lastIdx
                    for (int h = 0; h < archetype.Hot.Length; h++)
                    {
                        if (archetype.Hot[h].WarmTileIndex == lastIdx)
                        {
                            archetype.hot[h] = new RasterArchetype.HotTile { WarmTileIndex = warmIdx };
                        }
                    }
                }

                // Now shrink the list: lastIdx is gone
                archetype.Warm.RemoveAtSwapBack(warmIdx);

                // And remove the mapping for the tile we're freezing
                warmIndexByTile.Remove(tileIndex);
            }
        }
        
        private void OnColdDealloc()
        {
            archetype.Clear();
        }
    }
}