using System;
using Netherlands3D.Functionalities.Wms;
using Netherlands3D.Tilekit.ContentLoaders;
using Netherlands3D.Tilekit.Renderers;
using Netherlands3D.Tilekit.TileSetBuilders;
using Netherlands3D.Tilekit.TileSets;
using Netherlands3D.Tilekit.WriteModel;
using Unity.Collections;
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
        public int depth = 4;
        private const int InitialCapacity = 1024;
        
        public TextureDecalProjector textureDecalProjectorPrefab;
        public string Url;

        private Texture2DLoader importer;
        private Texture2DOverlayRenderer tileRenderer;
        protected QuadTreeBuilder builder;
        private BoxBoundingVolume AreaOfInterest => BoxBoundingVolume.FromTopLeftAndBottomRight(new double3(left, top, 0), new double3(right, bottom, 0));

        protected override void Initialize()
        {
            builder ??= new WmsTileSetBuilder(Url);
            importer = new Texture2DLoader();
            tileRenderer = new Texture2DOverlayRenderer(new DecalProjectorPool(textureDecalProjectorPrefab, gameObject));
            OnColdAlloc();
        }

        protected override RasterTileSet CreateTileSet() => new(AreaOfInterest, InitialCapacity);

        private void OnColdAlloc()
        {
            builder.Build(tileSet, new() { Depth = this.depth });
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
            tileRenderer.Create(tileSet.GetTile(tileIndex), tex);
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
            for (var index = 0; index < candidateTileIndices.Length; index++)
            {
                var tileIndex = candidateTileIndices[index];
                var tileWarmIndex = tileSet.Warm.IndexOf(tileIndex);

                // If evicting failed - continue and try again next cycle
                if (!importer.TryEvict(tileSet.TextureRef[tileWarmIndex])) continue;
                
                // Reorder texture refs
                for (int i = tileWarmIndex; i < tileSet.Warm.Length; i++)
                {
                    tileSet.TextureRef[i -1] = tileSet.TextureRef[i];
                }
                // Clear last ref because we are going to remove the tile from tileSet.Warm
                tileSet.TextureRef[tileSet.Warm.Length] = ulong.MaxValue;
    
                // Remove it - this will also reorder all entries after tileWarmIndex - hence the prior action
                tileSet.Warm.RemoveAt(tileWarmIndex);
            }
        }

        private void OnColdDealloc()
        {
            tileSet.Clear();
        }
    }
}