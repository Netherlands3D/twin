using System;
using System.Collections.Generic;
using System.Text;
using Netherlands3D.Functionalities.Wms;
using Netherlands3D.Tilekit.Archetypes;
using Netherlands3D.Tilekit.ContentLoaders;
using Netherlands3D.Tilekit.Renderers;
using Netherlands3D.Tilekit.ColdStorageMaterializers;
using Netherlands3D.Tilekit.WriteModel;
using Unity.Mathematics;
using UnityEngine;

namespace Netherlands3D.Tilekit.DataSets
{
    // https://docs.ogc.org/cs/22-025r4/22-025r4.html#toc31 for implicit tiling inspiration
    public class WmsDataSet : DataSet<RasterArchetype, RasterArchetype.WarmTile, RasterArchetype.HotTile>, ITileLifecycleBehaviour
    {
        private const int InitialCapacity = 1024;

        public TextureDecalProjector textureDecalProjectorPrefab;

        public string CapabilitiesUrl;
        public string Url;
        private Texture2DLoader importer;
        private Texture2DOverlayRenderer tileRenderer;

        private readonly Dictionary<int, int> warmIndexByTile = new(); // tileIndex -> index in warmTiles

        // https://service.pdok.nl/prorail/spoorwegen/wms/v1_0?request=GetCapabilities&service=WMS
        // https://service.pdok.nl/prorail/spoorwegen/wms/v1_0?request=GetMap&service=WMS&version=1.3.0&layers=kilometrering&styles=kilometrering&CRS=EPSG%3a28992&bbox=155000%2c464000%2c156000%2c465000&width=1024&height=1024&format=image%2fpng&transparent=true

        // https://service.pdok.nl/kadaster/kadastralekaart/wms/v5_0?request=GetCapabilities&service=WMS
        // https://service.pdok.nl/kadaster/kadastralekaart/wms/v5_0?request=GetMap&service=WMS&version=1.3.0&layers=OpenbareRuimteNaam&styles=standaard%3aopenbareruimtenaam&CRS=EPSG%3a28992&width=1024&height=1024&format=image%2fpng&transparent=true&bbox=154000%2c462000%2c155000%2c463000

        protected override void Initialize()
        {
            Url = "https://service.pdok.nl/prorail/spoorwegen/wms/v1_0?request=GetMap&service=WMS&version=1.3.0&layers={layers}&styles={styles}&CRS=EPSG%3a28992&width=1024&height=1024&format=image%2fpng&transparent=true&bbox={bbox}";
            importer = new Texture2DLoader();
            tileRenderer = new Texture2DOverlayRenderer(new DecalProjectorPool(textureDecalProjectorPrefab, gameObject));
            OnColdAlloc();
        }

        protected override RasterArchetype CreateArchetype() => new (AreaOfInterest, InitialCapacity);

        private void OnColdAlloc()
        {
            // var capabilities = new WmsGetCapabilities(new Uri(CapabilitiesUrl), response.Data as string);
            //
            // var tileSet = new TileSet();
            // var bbox = capabilities.GetBounds().GlobalBoundingBox;

            new ExplicitQuadTreeMaterializer().Materialize(archetype.Cold, new() { Depth = 4});
        }

        private static BoxBoundingVolume AreaOfInterest
        {
            get
            {
                int left = 153000;
                int right = 158000;
                int top = 462000;
                int bottom = 467000;

                var areaOfInterest = BoxBoundingVolume.FromTopLeftAndBottomRight(
                    new double3(left, top, 0),
                    new double3(right, bottom, 0)
                );
                return areaOfInterest;
            }
        }
        
        public override void OnWarmUp(ReadOnlySpan<int> candidateTileIndices)
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
                var key = Texture2DLoader.HashUrl(url);

                var warmIdx = archetype.Warm.Length;
                archetype.Warm.AddNoResize(new RasterArchetype.WarmTile { TileIndex = candidate, TextureKey = key });
                warmIndexByTile[candidate] = warmIdx;

                importer.Load(url);
            }

            OnHeatUp(candidateTileIndices);
        }

        public override void OnHeatUp(ReadOnlySpan<int> candidateTileIndices)
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

        public override void OnCooldown(ReadOnlySpan<int> candidateTileIndices)
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

        public override void OnFreeze(ReadOnlySpan<int> candidateTileIndices)
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