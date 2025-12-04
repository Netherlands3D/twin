using System;
using Netherlands3D.Functionalities.Wms;
using Netherlands3D.Tilekit.ContentLoaders;
using Netherlands3D.Tilekit.Renderers;
using Netherlands3D.Tilekit.TileSetMaterializers;
using Netherlands3D.Tilekit.TileSets;
using Netherlands3D.Tilekit.WriteModel;
using Unity.Mathematics;
using UnityEngine;

namespace Netherlands3D.Tilekit.DataSets
{
    public abstract class RasterDataSet : DataSet<RasterTileSet>
    {
        public int left = 153000;
        public int right = 158000;
        public int top = 462000;
        public int bottom = 467000;
        private const int InitialCapacity = 1024;
        
        public TextureDecalProjector textureDecalProjectorPrefab;
        public string Url;

        private Texture2DLoader importer;
        private Texture2DOverlayRenderer tileRenderer;
        private ExplicitQuadTreeMaterializer materializer;
        private BoxBoundingVolume AreaOfInterest => BoxBoundingVolume.FromTopLeftAndBottomRight(new double3(left, top, 0), new double3(right, bottom, 0));

        protected override void Initialize()
        {
            materializer = new ExplicitQuadTreeMaterializer();
            importer = new Texture2DLoader();
            tileRenderer = new Texture2DOverlayRenderer(new DecalProjectorPool(textureDecalProjectorPrefab, gameObject));
            OnColdAlloc();
        }

        protected override RasterTileSet CreateTileSet() => new(AreaOfInterest, InitialCapacity);

        private void OnColdAlloc()
        {
            materializer.Materialize(tileSet, new() { Depth = 4 });
        }
        
        public override void OnWarmUp(ReadOnlySpan<int> candidateTileIndices)
        {
            for (var index = 0; index < candidateTileIndices.Length; index++)
            {
                var tile = new RasterTileSet.Tile(tileSet, candidateTileIndices[index]);

                WarmTile(tile);
            }

            OnHeatUp(candidateTileIndices);
        }

        protected abstract string GetImageUrl(RasterTileSet.Tile tile);

        private void WarmTile(RasterTileSet.Tile tile)
        {
            var url = GetImageUrl(tile);
            Debug.Log($"Warming tile {tile.Index} with url {url}");

            var warmIdx = tileSet.WarmTile(tile.Index);
            tileSet.TextureRef[warmIdx] = Texture2DLoader.HashUrl(url);
            importer.Load(url);
        }

        public override void OnHeatUp(ReadOnlySpan<int> candidateTileIndices)
        {
            for (int i = 0; i < candidateTileIndices.Length; i++)
            {
                HeatUpTile(candidateTileIndices[i]);
            }
        }

        private void HeatUpTile(int tileIndex)
        {
            var tile = new RasterTileSet.Tile(tileSet, tileIndex);

            // If it aint' warm - it cannot get hot
            if (!tile.IsWarm) return;

            // TODO: shouldn't this be a condition to go to hot? Instead of waiting while hot? or is this better so that we can show a placeholder?
            // TODO: Check if tiles have an empty texture after moving this to the warm phase, and immediately discard the texture and prevent this tile
            //   from using rendered representation
                
            if (!importer.TryGet(tile.Texture2DRef, out var tex))
            {
                // When no texture is found - try again
                importer.GetAsync(tile.Texture2DRef).Then(_ => HeatUpTile(tileIndex));
                return;
            }

            tileSet.HeatTile(tileIndex);
            tileRenderer.Create(tileSet.Get(tileIndex), tex);
        }

        public override void OnCooldown(ReadOnlySpan<int> candidateTileIndices)
        {
            for (int i = 0; i < candidateTileIndices.Length; i++)
            {
                CooldownTile(candidateTileIndices[i]);
            }
        }

        private void CooldownTile(int tileIndex)
        {
            tileRenderer.Release(tileIndex);

            for (var index = 0; index < tileSet.Hot.Length; index++)
            {
                var hotTile = tileSet.Hot[index];
                if (tileSet.Hot[hotTile] != tileIndex) continue;

                tileSet.Hot.RemoveAtSwapBack(index);
                index--;
            }
        }

        public override void OnFreeze(ReadOnlySpan<int> candidateTileIndices)
        {
            // TODO: Write this again as soon as DataSet/TileSet refactor is done
        }

        private void OnColdDealloc()
        {
            tileSet.Clear();
        }
    }
}