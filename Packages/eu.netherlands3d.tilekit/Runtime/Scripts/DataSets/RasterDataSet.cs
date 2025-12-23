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

        // Voorbeeld interessegebied: bestrijkt heel Provincie Flevoland
        public int left = 133900;
        public int right = 189100;
        public int top = 514500;
        public int bottom = 472500;
        
        // De diepte bepaalt het aantal tegels
        public int depth = 5;
        
        public TextureDecalProjector textureDecalProjectorPrefab;
        public string Url;

        private Texture2DOverlayRenderer tileRenderer;
        private BoxBoundingVolume AreaOfInterest => BoxBoundingVolume.FromTopLeftAndBottomRight(new double3(left, top, 0), new double3(right, bottom, 0));

        // TODO: We now have one tileSet, but it is worth it to allow for regions in a dataset, where each region has its own tileSet.
        //     Regions allow for smaller area's of interest and seamless transitions between regions. It can also be interesting to 
        //     model regions in a pooling fashion so that you can pool them and swap them between datasets as needed
        protected override RasterTileSet CreateTileSet() => new(AreaOfInterest, (int)Math.Pow(4, depth + 1));

        protected abstract QuadTreeBuilder CreateTileSetBuilder();

        protected override void OnInitialize()
        {
            QuadTreeBuilder builder = CreateTileSetBuilder();
            builder.Build(TileSet, new() { Depth = this.depth });
            tileRenderer = new Texture2DOverlayRenderer(new DecalProjectorPool(textureDecalProjectorPrefab, gameObject));
        }

        public override void OnWarmUp(ReadOnlySpan<int> candidateTileIndices)
        {
            for (var index = 0; index < candidateTileIndices.Length; index++)
            {
                var tile = new RasterTileSet.Tile(TileSet, candidateTileIndices[index]);

                var url = tile.Contents()[0].Uri();

                // TODO: The WarmTile method assumed an AppendOnly structure for WarmTiles, but they are not - they are reused.
                //   Also: TextureRef uses the WarmIdx - but if that is not append-only, we need to ensure it is not renumbered when
                //   removing elements.
                // TODO: Or is this not a problem because OnFreeze reorders?
                TileSet.LoadTexture(TileSet.WarmTile(tile.Index), url);
            }
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
            // // If it aint' warm - it cannot get hot
            if (TileSet.Warm.Contains(tileIndex)) return;
            
            // // TODO: shouldn't this be a condition to go to hot? Instead of waiting while hot? or is this better so that we can show a placeholder?
            // // TODO: Check if tiles have an empty texture after moving this to the warm phase, and immediately discard the texture and prevent this tile
            // //   from using rendered representation
            //     
            // if (!importer.TryGet(tile.Texture2DRef, out var tex))
            // {
            //     // When no texture is found - try again
            //     importer.GetAsync(tile.Texture2DRef).Then(_ => HeatUpTile(tileIndex));
            //     return;
            // }
            //
            TileSet.HeatTile(tileIndex);
            // tileRenderer.Create(TileSet.GetTile(tileIndex), tex);
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
            TileSet.CoolTile(tileIndex);
            // tileRenderer.Release(tileIndex);
        }

        public override void OnFreeze(ReadOnlySpan<int> candidateTileIndices)
        {
            for (var index = 0; index < candidateTileIndices.Length; index++)
            {
                FreezeTile(candidateTileIndices[index]);
            }
        }

        private void FreezeTile(int tileIndex)
        {
            // If unloading failed - return and try again next cycle
            if (!TileSet.UnloadTexture(TileSet.Warm.IndexOf(tileIndex))) return;

            // Remove it - this will also reorder all entries after tileWarmIndex - this is captured in UnloadTexture
            TileSet.FreezeTile(tileIndex);
        }
    }
}