using System;
using System.IO;
using Netherlands3D.Tilekit.WriteModel;
using Newtonsoft.Json;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Netherlands3D.Tilekit.TileBuilders
{
    public partial class Ogc3DTilesHydrator : IColdStorageHydrator<Ogc3DTilesHydratorSettings>
    {
        public void Build(ColdStorage storage, Ogc3DTilesHydratorSettings settings)
        {
            Debug.Log("1. Building tileset from");
            var jsonSerializerSettings = new JsonSerializerSettings
            {
                DateParseHandling = DateParseHandling.None,
                FloatParseHandling = FloatParseHandling.Double,
                Formatting = Formatting.None,
                Converters = { new TileDtoAoiConverter(storage.AreaOfInterest) },
                NullValueHandling = NullValueHandling.Ignore,
            };

            Debug.Log("2. Building tileset from");
            var serializer = JsonSerializer.Create(jsonSerializerSettings);

            using var sr = new StreamReader(settings.Stream);
            using var reader = new JsonTextReader(sr)
            {
                CloseInput = false,
            };
            Debug.Log("3. Building tileset from");

            TilesetDto tileset;
            try
            {
                // This will stream-deserialize through your converter
                tileset = serializer.Deserialize<TilesetDto>(reader);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                throw;
            }

            Debug.Log("4. Building tileset from");
            
            if (tileset == null)
                throw new InvalidOperationException("Failed to deserialize tileset.json");
            Debug.Log("5. Building tileset from");

            storage.Clear();
            HydrateTile(storage, tileset.Root);
        }

        private int HydrateTile(ColdStorage storage, TileDto tile)
        {
            var boxBoundingVolume = tile.BoundingVolume.ToBoxBoundingVolume();
            if (!boxBoundingVolume.HasValue)
            {
                // Invalid tile - so return a -1 to indicate that it is invalid
                return -1;
            }

            // We know how many children we have, so we can pre-allocate the array - and after adding the children we know their indices
            // and update the store.
            var childIndices = new NativeArray<int>(tile.Children.Count, Allocator.Temp);

            int tileIndex = storage.AddTile(
                boxBoundingVolume.Value,
                tile.GeometricError,
                ReadOnlySpan<TileContentData>.Empty, // TODO: populate contents
                childIndices,
                tile.Refine == "REPLACE" ? MethodOfRefinement.Replace : MethodOfRefinement.Add,
                SubdivisionScheme.None,
                float4x4.identity
            );

            // Create the children and capture their tile ids
            for (var index = 0; index < tile.Children.Count; index++)
            {
                childIndices[index] = HydrateTile(storage, tile.Children[index]);
            }

            // Update the tile's children
            storage.Children[tileIndex].Replace(childIndices);

            childIndices.Dispose();

            return tileIndex;
        }
    }

    public struct Ogc3DTilesHydratorSettings
    {
        public Stream Stream;
    }
}